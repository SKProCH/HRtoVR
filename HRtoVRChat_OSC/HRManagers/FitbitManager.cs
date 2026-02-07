using HRtoVRChat_OSC;

namespace HRtoVRChat_OSC.HRManagers;

public class FitbitManager : HRManager {
    private Thread _thread;
    private WebsocketTemplate wst;

    private CancellationTokenSource tokenSource = new();

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
        LogHelper.Log("Initialized WebSocket!");
        return IsConnected;
    }

    public string GetName() {
        return "FitbitHRtoWS";
    }

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        tokenSource.Cancel();
        if (wst != null) {
             LogHelper.Debug("Sent message to Stop WebSocket");
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
            wst = new WebsocketTemplate(url);
            wst.OnMessage = HandleMessage;

            var noerror = true;
            try {
                await wst.Start();
            }
            catch (Exception e) {
                LogHelper.Error("Failed to connect to Fitbit Server! Exception: ", e);
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
                    LogHelper.Error("Failed to Close connection with the Fitbit Server! Exception: ", e);
                }
            }
            else
                LogHelper.Warn("WebSocket is not alive! Did you mean to Dispose()?");
        }
        else
            LogHelper.Warn("WebSocket is null! Did you mean to Initialize()?");
    }
}
