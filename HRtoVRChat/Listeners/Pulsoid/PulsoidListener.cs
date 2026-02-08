using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.Pulsoid;

public class PulsoidListener : IHrListener {
    private WebsocketClient? _client;
    protected readonly ILogger _logger;
    protected readonly BehaviorSubject<int> _heartRate = new(0);
    protected readonly BehaviorSubject<bool> _isConnected = new(false);
    protected readonly IOptionsMonitor<PulsoidOptions> _options;
    protected IDisposable? _optionsSubscription;

    public PulsoidListener(ILogger<PulsoidListener> logger, IOptionsMonitor<PulsoidOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    // Protected constructor for Stromno
    protected PulsoidListener(ILogger logger, IOptionsMonitor<PulsoidOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    private bool IsClientRunning => _client?.IsRunning ?? false;

    public string Timestamp { get; private set; } = string.Empty;

    public virtual void Start() {
        _optionsSubscription = _options.OnChange(opt =>
        {
            _logger.LogInformation("Pulsoid configuration changed, restarting...");
            Stop();
            Start();
        });
        StartConnection(_options.CurrentValue.Widget);
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
            _logger.LogInformation("Reconnection happened, type: {ReconnectionType}", info.Type);
            SendSubscription(id);
            _isConnected.OnNext(true);
        });

        _client.DisconnectionHappened.Subscribe(_ => _isConnected.OnNext(false));

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
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    public void Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogInformation("Stopped Pulsoid/Stromno WebSocket");
    }

    private void HandleMessage(string message) {
        try {
            // Parse the message and get the HR or Pong
            var jo = JObject.Parse(message);
            if (jo["method"] != null) {
                var pingId = jo["pingId"]?.Value<string>();
                _client?.Send("{\"method\": \"pong\", \"pingId\": \"" + pingId + "\"}");
            }
            else {
                _heartRate.OnNext(Convert.ToInt32(jo["hr"]?.Value<string>()));
                Timestamp = jo["timestamp"]?.Value<string>() ?? string.Empty;
                _isConnected.OnNext(true);
            }
        }
        catch (Exception) { }
    }
}
