using Newtonsoft.Json.Linq;

namespace HRtoVRChat_OSC.HRManagers;

public class HRProxyManager : HRManager {
    private Thread? _thread;
    private CancellationTokenSource tokenSource = new();
    private WebsocketTemplate? wst;

    private bool IsConnected {
        get {
            if (wst != null) {
                return wst.IsAlive;
            }

            return false;
        }
    }

    public int HR { get; private set; }
    public string Timestamp { get; private set; }

    public bool Init(string id) {
        tokenSource = new CancellationTokenSource();
        StartThread(id);
        LogHelper.Log("Initialized WebSocket!");
        return IsConnected;
    }

    public string GetName() {
        return "HRProxy";
    }

    public int GetHR() {
        return HR;
    }

    public void Stop() {
        tokenSource.Cancel();
    }

    public bool IsOpen() {
        return IsConnected && HR > 0;
    }

    public bool IsActive() {
        return IsConnected;
    }

    private async void HandleMessage(string message) {
        try {
            // Parse the message and get the HR or Pong
            var jo = JObject.Parse(message);
            if (jo["method"] != null) {
                var pingId = jo["pingId"]?.Value<string>();
                await wst.SendMessage("{\"method\": \"pong\", \"pingId\": \"" + pingId + "\"}");
            }
            else {
                HR = Convert.ToInt32(jo["hr"].Value<string>());
                Timestamp = jo["timestamp"].Value<string>();
            }
        }
        catch (Exception) { }
    }

    public void StartThread(string id) {
        _thread = new Thread(async () => {
            wst = new WebsocketTemplate("wss://hrproxy.fortnite.lol:2096/hrproxy");
            var noerror = true;
            try {
                await wst.Start();
            }
            catch (Exception e) {
                LogHelper.Error("Failed to connect to HypeRate server!", e);
                noerror = false;
            }

            if (noerror) {
                await wst.SendMessage("{\"reader\": \"HRProxy\", \"identifier\": \"" + id + "\"}");
                while (!tokenSource.IsCancellationRequested) {
                    if (IsConnected) {
                        var message = await wst.ReceiveMessage();
                        if (!string.IsNullOrEmpty(message))
                            HandleMessage(message);
                    }
                    else {
                        // Stop and Restart
                        HRService.RestartHRListener();
                    }

                    Thread.Sleep(1);
                }
            }

            await Close();
            LogHelper.Log("Closed HRProxy");
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
                    LogHelper.Error("Failed to close connection to HRProxy Server! Exception: ", e);
                }
            }
            else
                LogHelper.Warn("WebSocket is not alive! Did you mean to Dispose()?");
        }
        else
            LogHelper.Warn("WebSocket is null! Did you mean to Initialize()?");
    }
}