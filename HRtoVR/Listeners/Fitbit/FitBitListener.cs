using System;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Websocket.Client;

namespace HRtoVR.Listeners.Fitbit;

public class FitBitListener : IHrListener {
    private WebsocketClient? _client;
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly ILogger<FitBitListener> _logger;
    private readonly IOptionsMonitor<FitBitOptions> _options;
    private IDisposable? _optionsSubscription;

    public FitBitListener(ILogger<FitBitListener> logger, IOptionsMonitor<FitBitOptions> options) {
        _logger = logger;
        _options = options;
    }

    public async Task Start() {
        _optionsSubscription = _options.OnChange(async opt => {
            _logger.LogInformation("Fitbit configuration changed, restarting...");
            await Stop();
            await Start();
        });
        var url = _options.CurrentValue.Url;
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri(url), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));

        // Start sending pings/requests once connected/reconnected
        _client.ReconnectionHappened.Subscribe(info => {
            _logger.LogInformation("Reconnection happened, type: {ReconnectionType}", info.Type);
            StartPolling();
        });

        try {
            await _client.Start();
            StartPolling();
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to connect to Fitbit Server!");
        }

        _logger.LogInformation("Initialized Fitbit WebSocket!");
    }

    // Polling mechanism to replace the old loop
    private CancellationTokenSource? _pollingCts;

    private void StartPolling() {
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;

        Task.Run(async () => {
            while (!token.IsCancellationRequested && (_client?.IsRunning ?? false)) {
                try {
                    _client?.Send("getHR");
                    _client?.Send("checkFitbitConnection");
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception e) {
                    _logger.LogWarning(e, "Error during polling");
                }
            }
        }, token);
    }

    public string Name => "FitBit";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    public Task Stop() {
        _optionsSubscription?.Dispose();
        _optionsSubscription = null;
        _pollingCts?.Cancel();
        _client?.Dispose();
        _client = null;
        _heartRate.OnNext(0);
        _isConnected.OnNext(false);
        _logger.LogDebug("Stopped Fitbit WebSocket");
        return Task.CompletedTask;
    }

    private void HandleMessage(string msg) {
        if (msg.Contains("yes"))
            _isConnected.OnNext(true);
        else if (msg.Contains("no"))
            _isConnected.OnNext(false);
        else if (int.TryParse(msg, out var hr))
            _heartRate.OnNext(hr);
    }
}