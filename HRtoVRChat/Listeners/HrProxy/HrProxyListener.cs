using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.HrProxy;

public class HrProxyListener : IHrListener {
    private CancellationTokenSource tokenSource = new();
    private WebsocketClient? _client;
    private readonly ILogger<HrProxyListener> _logger;
    private readonly HRProxyOptions _options;

    public HrProxyListener(ILogger<HrProxyListener> logger, IOptions<HRProxyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private bool IsConnected {
        get {
            if (_client != null) {
                return _client.IsRunning;
            }

            return false;
        }
    }

    public int HR { get; private set; }
    public string Timestamp { get; private set; } = string.Empty;

    public void Start() {
        tokenSource = new CancellationTokenSource();
        StartThread(_options.Id);
        _logger.LogInformation("Initialized WebSocket!");
    }

    public string Name => "HRProxy";

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        tokenSource.Cancel();
    }

    public bool IsOpen() {
        return IsConnected && HR > 0;
    }

    public bool IsActive() {
        return IsConnected;
    }

    private void HandleMessage(string message) {
        try {
            // Parse the message and get the HR or Pong
            var jo = JObject.Parse(message);
            if (jo["method"] != null) {
                var pingId = jo["pingId"]?.Value<string>();
                if (_client != null) _client.Send("{\"method\": \"pong\", \"pingId\": \"" + pingId + "\"}");
            }
            else {
                HR = Convert.ToInt32(jo["hr"]?.Value<string>());
                Timestamp = jo["timestamp"]?.Value<string>() ?? string.Empty;
            }
        }
        catch (Exception) { }
    }

    public void StartThread(string id) {
        var token = tokenSource.Token;
        Task.Run(async () => {
            _client = new WebsocketClient(new Uri("wss://hrproxy.fortnite.lol:2096/hrproxy"));
            _client.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));
            _client.ReconnectionHappened.Subscribe(info =>
            {
                _client.Send("{\"reader\": \"HRProxy\", \"identifier\": \"" + id + "\"}");
            });

            try {
                await _client.Start();
                // Send initial subscription
                _client.Send("{\"reader\": \"HRProxy\", \"identifier\": \"" + id + "\"}");
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to connect to HypeRate server!");
            }

            while (!token.IsCancellationRequested) {
                if (IsConnected) {
                    // Managed by Websocket.Client
                }
                else {
                    // Stop and Restart
                    // HRService.RestartHRListener();
                }

                try {
                    await Task.Delay(1000, token);
                } catch (TaskCanceledException) { break; }
            }

            await Close();
            _logger.LogInformation("Closed HRProxy");
        }, token);
    }

    private async Task Close() {
        if (_client != null) {
            if (_client.IsRunning) {
                try {
                    await _client.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty);
                    _client.Dispose();
                    _client = null;
                }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to close connection to HRProxy Server!");
                }
            }
            else
                _logger.LogWarning("WebSocket is not alive! Did you mean to Dispose()?");
        }
        else
            _logger.LogWarning("WebSocket is null! Did you mean to Initialize()?");
    }
}
