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
            await wst.Start();
            while (!shouldUpdate.IsCancellationRequested) {
                // old method RIP ;(
                /*
                int parsedHR = default(int);
                try
                {
                    HttpResponseMessage response = await client.GetAsync(pubUrl);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Now Parse the Information
                    JObject jo = null;
                    try
                    {
                        jo = JObject.Parse(responseBody);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error("APIReaderManager", "Failed to parse JObject! Exception: " + e);
                    }
                    if (jo != null)
                        try
                        {
                            parsedHR = jo["bpm"].Value<int>();
                        }
                        catch (Exception)
                        {
                            // try and just parse the raw text
                            try
                            {
                                parsedHR = Convert.ToInt32(responseBody);
                            }
                            catch (Exception){}
                        }
                }
                catch (HttpRequestException e)
                {
                    LogHelper.Error("APIReaderManager", "Failed to get HttpRequest! Exception: " + e);
                }
                HR = parsedHR;
                */
                var parsedHR = default(int);
                if (wst != null && wst.IsAlive) {
                    // Update stuff
                    var data = await wst.ReceiveMessage();
                    if (!string.IsNullOrEmpty(data)) {
                        // Parse HR
                        JObject jo = null;
                        try {
                            jo = JObject.Parse(data);
                        }
                        catch (Exception e) {
                            LogHelper.Error("Failed to parse JObject! Exception: " + e);
                        }

                        if (jo != null) {
                            try {
                                parsedHR = jo["data"]["heart_rate"].Value<int>();
                            }
                            catch (Exception) {
                                LogHelper.Error("Failed to parse Herat Rate!");
                            }
                        }
                    }
                }

                HR = parsedHR;
            }
        });
        _thread.Start();
    }
}