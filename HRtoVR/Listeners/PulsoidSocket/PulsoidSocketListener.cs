using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVR.Listeners.PulsoidSocket;

internal class PulsoidSocketListener : IHrListener {
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private WebsocketClient? _client;
    private readonly ILogger<PulsoidSocketListener> _logger;
    private readonly IOptionsMonitor<PulsoidSocketOptions> _options;
    private IDisposable? _optionsSubscription;

    public PulsoidSocketListener(ILogger<PulsoidSocketListener> logger, IOptionsMonitor<PulsoidSocketOptions> options) {
        _logger = logger;
        _options = options;
    }

    public async Task Start() {
        _optionsSubscription = _options.OnChange(async opt => {
            _logger.LogInformation("PulsoidSocket configuration changed, restarting...");
            await Stop();
            await Start();
        });
        var pubUrl = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=" + _options.CurrentValue.Key;

        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri(pubUrl), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg => {
            var message = msg.Text;
            if (!string.IsNullOrEmpty(message)) {
                try {
                    var jo = JObject.Parse(message);
                    _heartRate.OnNext(jo["data"]?["heart_rate"]?.Value<int>() ?? 0);
                    _isConnected.OnNext(true);
                }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to parse Pulsoid message");
                }
            }
        });

        _client.ReconnectionHappened.Subscribe(_ => _isConnected.OnNext(true));
        _client.DisconnectionHappened.Subscribe(_ => _isConnected.OnNext(false));

        await _client.Start();
        _logger.LogInformation("PulsoidSocketListener started");
    }

    public Task Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogInformation("PulsoidSocketListener stopped");
        return Task.CompletedTask;
    }

    public string Name => "PulsoidSocket";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
}