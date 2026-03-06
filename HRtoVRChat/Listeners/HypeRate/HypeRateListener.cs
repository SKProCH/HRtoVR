using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure.Options;
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
    private IDisposable? _optionsSubscription;

    public HypeRateListener(ILogger<HypeRateListener> logger, IOptionsManager<HypeRateOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task Start() {
        _optionsSubscription = _options.OnChange(async opt =>
        {
            _logger.LogInformation("HypeRate configuration changed, restarting...");
            await Stop();
            await Start();
        });
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

        try
        {
            await _client.Start();
            SendSubscription(id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start HypeRate WebSocket");
        }

        _logger.LogInformation("Initialized HypeRate WebSocket!");
    }

    private void SendSubscription(string id)
    {
        _client?.Send($$"""{"reader": "hyperate", "identifier": "{{id}}", "service": "vrchat"}""");
    }

    public string Name => "HypeRate";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    public Task Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogInformation("Stopped HypeRate WebSocket");
        return Task.CompletedTask;
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
