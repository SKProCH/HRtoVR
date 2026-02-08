using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.PulsoidSocket;

internal class PulsoidSocketListener : IHrListener {
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private WebsocketClient? _client;
    private readonly ILogger<PulsoidSocketListener> _logger;
    private readonly IOptionsMonitor<PulsoidSocketOptions> _options;
    private IDisposable? _optionsSubscription;

    public PulsoidSocketListener(ILogger<PulsoidSocketListener> logger, IOptionsMonitor<PulsoidSocketOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public void Start() {
        _optionsSubscription = _options.OnChange(opt =>
        {
            _logger.LogInformation("PulsoidSocket configuration changed, restarting...");
            Stop();
            Start();
        });
        var pubUrl = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=" + _options.CurrentValue.Key;

        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri(pubUrl), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg =>
        {
            var message = msg.Text;
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    var jo = JObject.Parse(message);
                    _heartRate.OnNext(jo["data"]?["heart_rate"]?.Value<int>() ?? 0);
                    _isConnected.OnNext(true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to parse Pulsoid message");
                }
            }
        });

        _client.ReconnectionHappened.Subscribe(_ => _isConnected.OnNext(true));
        _client.DisconnectionHappened.Subscribe(_ => _isConnected.OnNext(false));

        _client.Start();
        _logger.LogInformation("PulsoidSocketListener started");
    }

    public void Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogInformation("PulsoidSocketListener stopped");
    }

    public string Name => "PulsoidSocket";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
}
