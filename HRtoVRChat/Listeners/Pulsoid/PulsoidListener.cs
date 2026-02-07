using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.Pulsoid;

public class PulsoidListener : IHrListener {
    private WebsocketClient? _client;
    protected readonly ILogger _logger;
    private readonly PulsoidOptions _options;

    public PulsoidListener(ILogger<PulsoidListener> logger, IOptions<PulsoidOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    // Protected constructor for Stromno
    protected PulsoidListener(ILogger logger)
    {
        _logger = logger;
        _options = new PulsoidOptions();
    }

    private bool IsConnected => _client?.IsRunning ?? false;

    public int HR { get; private set; }
    public string Timestamp { get; private set; } = string.Empty;

    public virtual void Start() {
        StartConnection(_options.Widget);
        _logger.LogInformation("Initialized Pulsoid WebSocket!");
    }

    protected void StartConnection(string id) {
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
            if (t.IsFaulted) _logger.LogError(t.Exception, "Failed to start Pulsoid/Stromno WebSocket");
            else SendSubscription(id);
        });
    }

    private void SendSubscription(string id)
    {
        _client?.Send("{\"reader\": \"pulsoid\", \"identifier\": \"" + id + "\", \"service\": \"vrchat\"}");
    }

    public virtual string Name => "Pulsoid";

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        _client?.Dispose();
        _client = null;
        HR = 0;
        _logger.LogInformation("Stopped Pulsoid/Stromno WebSocket");
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
