using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Websocket.Client;

namespace HRtoVRChat.Listeners.Fitbit;

public class FitBitListener : IHrListener {
    private WebsocketClient? _client;
    private readonly ILogger<FitBitListener> _logger;
    private readonly FitbitOptions _options;

    public FitBitListener(ILogger<FitBitListener> logger, IOptions<FitbitOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private bool IsConnected => _client?.IsRunning ?? false;

    public bool FitbitIsConnected { get; private set; }
    public int HR { get; private set; }

    public void Start() {
        var url = _options.Url;
        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri(url), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg => HandleMessage(msg.Text));

        // Start sending pings/requests once connected/reconnected
        _client.ReconnectionHappened.Subscribe(info =>
        {
            _logger.LogInformation($"Reconnection happened, type: {info.Type}");
            StartPolling();
        });

        _client.Start().ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to connect to Fitbit Server!");
            else
                StartPolling();
        });

        _logger.LogInformation("Initialized Fitbit WebSocket!");
    }

    // Polling mechanism to replace the old loop
    private CancellationTokenSource? _pollingCts;

    private void StartPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && IsConnected)
            {
                try
                {
                    _client?.Send("getHR");
                    _client?.Send("checkFitbitConnection");
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error during polling");
                }
            }
        }, token);
    }

    public string Name => "FitbitHRtoWS";

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        _pollingCts?.Cancel();
        _client?.Dispose();
        _client = null;
        HR = 0;
        FitbitIsConnected = false;
        _logger.LogDebug("Stopped Fitbit WebSocket");
    }

    public bool IsOpen() {
        return IsConnected && FitbitIsConnected;
    }

    public bool IsActive() {
        return IsConnected;
    }

    private void HandleMessage(string msg) {
        if (msg.Contains("yes"))
            FitbitIsConnected = true;
        else if (msg.Contains("no"))
            FitbitIsConnected = false;
        else
            try { HR = Convert.ToInt32(msg); }
            catch (Exception) { }
    }
}
