using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HRtoVRChat.Listeners;

public class FitBitListener : IHrListener {
    private Thread? _thread;
    private WebsocketTemplate? wst;
    private readonly ILogger<FitBitListener> _logger;

    private CancellationTokenSource tokenSource = new();

    public FitBitListener(ILogger<FitBitListener> logger)
    {
        _logger = logger;
    }

    private bool IsConnected {
        get {
            if (wst != null) {
                return wst.IsAlive;
            }

            return false;
        }
    }

    public bool FitbitIsConnected { get; private set; }
    public int HR { get; private set; }

    public bool Init(string url) {
        tokenSource = new CancellationTokenSource();
        StartThread(url);
        _logger.LogInformation("Initialized WebSocket!");
        return IsConnected;
    }

    public string Name => "FitbitHRtoWS";

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        tokenSource.Cancel();
        if (wst != null) {
             _logger.LogDebug("Sent message to Stop WebSocket");
             // wst.Stop() is called in thread or we can call it here if we want to be sure
        }
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

    public void StartThread(string url) {
        _thread = new Thread(async () => {
            wst = new WebsocketTemplate(url, _logger);
            wst.OnMessage = HandleMessage;

            var noerror = true;
            try {
                await wst.Start();
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to connect to Fitbit Server!");
                noerror = false;
            }

            if (noerror) {
                while (!tokenSource.IsCancellationRequested) {
                    if (IsConnected)
                    {
                        await wst.SendMessage("getHR");
                        await wst.SendMessage("checkFitbitConnection");
                    }
                    else
                    {
                        // Maybe try to reconnect or just wait?
                        // Websocket.Client handles reconnection usually, but we check IsConnected property from wst
                    }
                    Thread.Sleep(500);
                }
            }

            await Close();
        });
        _thread.Start();
    }

    private async Task Close() {
        if (wst != null) {
            if (wst.IsAlive) {
                try {
                    await wst.Stop();
                    wst = null;
                }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to Close connection with the Fitbit Server!");
                }
            }
            else
                _logger.LogWarning("WebSocket is not alive! Did you mean to Dispose()?");
        }
        else
            _logger.LogWarning("WebSocket is null! Did you mean to Initialize()?");
    }
}
