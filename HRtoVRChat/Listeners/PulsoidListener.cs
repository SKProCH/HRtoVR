using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace HRtoVRChat.Listeners;

public class PulsoidListener : IHrListener {
    private CancellationTokenSource tokenSource = new();
    private WebsocketTemplate? wst;
    private readonly ILogger<PulsoidListener> _logger;

    public PulsoidListener(ILogger<PulsoidListener> logger)
    {
        _logger = logger;
    }

    private bool IsConnected {
        get => wst != null && wst.IsAlive;
    }

    public int HR { get; private set; }
    public string Timestamp { get; private set; } = string.Empty;

    public bool Init(string id) {
        tokenSource = new CancellationTokenSource();
        StartThread(id);
        _logger.LogInformation("Initialized WebSocket!");
        return IsConnected;
    }

    public string Name => "Pulsoid (DEPRECATED)";

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

    private async void HandleMessage(string message) {
        try {
            // Parse the message and get the HR or Pong
            var jo = JObject.Parse(message);
            if (jo["method"] != null) {
                var pingId = jo["pingId"]?.Value<string>();
                if (wst != null) await wst.SendMessage("{\"method\": \"pong\", \"pingId\": \"" + pingId + "\"}");
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
            wst = new WebsocketTemplate("wss://hrproxy.fortnite.lol:2096/hrproxy", _logger);
            wst.OnMessage = HandleMessage;
            wst.OnReconnect = () =>
            {
                Task.Run(async () => {
                    if (wst != null) await wst.SendMessage("{\"reader\": \"pulsoid\", \"identifier\": \"" + id +
                                                           "\", \"service\": \"vrchat\"}");
                });
            };
            var noerror = true;
            try {
                await wst.Start();
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to connect to Pulsoid server!");
                noerror = false;
            }

            if (noerror) {
                if (wst != null) await wst.SendMessage("{\"reader\": \"pulsoid\", \"identifier\": \"" + id +
                                      "\", \"service\": \"vrchat\"}");
                while (!token.IsCancellationRequested) {
                    if (IsConnected) {
                        // Managed by Websocket.Client
                    }
                    else {
                        // Restart
                        // HRService.RestartHRListener();
                    }

                    try {
                        await Task.Delay(1000, token);
                    } catch (TaskCanceledException) { break; }
                }
            }

            await Close();
            _logger.LogInformation("Closed Pulsoid");
        }, token);
    }

    private async Task Close() {
        if (wst != null) {
            if (wst.IsAlive) {
                try {
                    await wst.Stop();
                    wst = null;
                }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to close connection to Pulsoid Server!");
                }
            }
            else
                _logger.LogWarning("WebSocket is not alive! Did you mean to Dispose()?");
        }
        else
            _logger.LogWarning("WebSocket is null! Did you mean to Initialize()?");
    }
}
