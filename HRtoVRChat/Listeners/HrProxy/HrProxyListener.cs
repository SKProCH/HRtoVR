using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.HrProxy;

public class HrProxyListener : IHrListener {
    private WebsocketClient? _client;
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly ILogger<HrProxyListener> _logger;
    private readonly IOptionsMonitor<HRProxyOptions> _options;
    private IDisposable? _optionsSubscription;

    public HrProxyListener(ILogger<HrProxyListener> logger, IOptionsMonitor<HRProxyOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public string Name => "HRProxy";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    public void Start() {
        _optionsSubscription = _options.OnChange(opt =>
        {
            _logger.LogInformation("HRProxy configuration changed, restarting...");
            Stop();
            Start();
        });
        var id = _options.CurrentValue.Id;
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
            _client.Send("{\"reader\": \"HRProxy\", \"identifier\": \"" + id + "\"}");
            _isConnected.OnNext(true);
        });
        _client.DisconnectionHappened.Subscribe(_ => _isConnected.OnNext(false));

        _client.Start().ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to connect to HRProxy server!");
            else
                _client.Send("{\"reader\": \"HRProxy\", \"identifier\": \"" + id + "\"}");
        });

        _logger.LogInformation("Initialized HRProxy WebSocket!");
    }

    public void Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _client?.Dispose();
        _client = null;
        _isConnected.OnNext(false);
        _heartRate.OnNext(0);
        _logger.LogInformation("Stopped HRProxy WebSocket");
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
                _heartRate.OnNext(Convert.ToInt32(jo["hr"]?.Value<string>()));
                _isConnected.OnNext(true);
            }
        }
        catch (Exception) { }
    }
}
