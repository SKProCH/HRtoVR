using Newtonsoft.Json.Linq;

namespace HRtoVRChat_OSC.HRManagers;

internal class PulsoidSocketManager : HRManager {
    private Thread? _thread;
    private int HR;
    private string pubUrl = string.Empty;
    private CancellationTokenSource shouldUpdate = new();

    private WebsocketTemplate? wst;

    public bool Init(string url) {
        shouldUpdate = new CancellationTokenSource();
        pubUrl = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=" + url;
        StartThread();
        LogHelper.Log("PulsoidSocketManager Initialized!");
        return true;
    }

    public void Stop() {
        shouldUpdate.Cancel();
        VerifyClosedThread();
    }

    public string GetName() {
        return "Pulsoid";
    }

    public int GetHR() {
        return HR;
    }

    public bool IsOpen() {
        return (wst?.IsAlive ?? false) && HR > 0;
    }

    public bool IsActive() {
        return wst?.IsAlive ?? false;
    }

    private void VerifyClosedThread() {
        if (_thread != null) {
            if (_thread.IsAlive)
                Stop();
        }
    }

    private void StartThread() {
        VerifyClosedThread();
        _thread = new Thread(async () => {
            wst = new WebsocketTemplate(pubUrl);
            wst.OnMessage = (message) =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    // Parse HR
                    JObject jo = null;
                    try
                    {
                        jo = JObject.Parse(message);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error("Failed to parse JObject! Exception: " + e);
                    }

                    if (jo != null)
                    {
                        try
                        {
                            HR = jo["data"]["heart_rate"].Value<int>();
                        }
                        catch (Exception)
                        {
                            LogHelper.Error("Failed to parse Herat Rate!");
                        }
                    }
                }
            };

            await wst.Start();
            while (!shouldUpdate.IsCancellationRequested) {
                 Thread.Sleep(1000);
            }
        });
        _thread.Start();
    }
}