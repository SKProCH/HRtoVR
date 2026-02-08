using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.HypeRate;

public class HypeRateListener : IHrListener {
    private WebsocketClient? _client;
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly ILogger<HypeRateListener> _logger;
    private readonly IOptionsMonitor<HypeRateOptions> _options;

    public HypeRateListener(ILogger<HypeRateListener> logger, IOptionsMonitor<HypeRateOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public void Start() {
        var id = _options.CurrentValue.SessionId;
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
            if (t.IsFaulted) _logger.LogError(t.Exception, "Failed to start HypeRate WebSocket");
            else SendSubscription(id);
        });

        _logger.LogInformation("Initialized HypeRate WebSocket!");
    }

    private void SendSubscription(string id)
    {
        _client?.Send($$"""{"reader": "hyperate", "identifier": "{{id}}", "service": "vrchat"}""");
    }

    public string Name => "HypeRate";
    public object? Settings => _options.CurrentValue;
    public string? SettingsSectionName => "HypeRateOptions";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    public void Stop() {
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogInformation("Stopped HypeRate WebSocket");
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
                _isConnected.OnNext(true);
            }
        }
        catch (Exception) { }
    }
}
