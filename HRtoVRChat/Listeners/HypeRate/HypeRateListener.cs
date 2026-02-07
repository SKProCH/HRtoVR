using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.HypeRate;

public class HypeRateListener : IHrListener {
    private WebsocketClient? _client;
    private readonly ILogger<HypeRateListener> _logger;
    private readonly HypeRateOptions _options;

    public HypeRateListener(ILogger<HypeRateListener> logger, IOptions<HypeRateOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private bool IsConnected => _client?.IsRunning ?? false;

    public int HR { get; private set; }
    public string Timestamp { get; private set; } = string.Empty;

    public void Start() {
        var id = _options.SessionId;
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri("wss://hrproxy.fortnite.lol:2096/hrproxy"), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));

        _client.ReconnectionHappened.Subscribe(info =>
        {
            _logger.LogInformation($"Reconnection happened, type: {info.Type}");
            SendSubscription(id);
        });

        _client.Start().ContinueWith(t =>
        {
            if (t.IsFaulted) _logger.LogError(t.Exception, "Failed to start HypeRate WebSocket");
            else SendSubscription(id);
        });

        _logger.LogInformation("Initialized HypeRate WebSocket!");
    }

    private void SendSubscription(string id)
    {
        _client?.Send("{\"reader\": \"hyperate\", \"identifier\": \"" + id + "\", \"service\": \"vrchat\"}");
    }

    public string Name => "HypeRate";

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        _client?.Dispose();
        _client = null;
        HR = 0;
        _logger.LogInformation("Stopped HypeRate WebSocket");
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
}
