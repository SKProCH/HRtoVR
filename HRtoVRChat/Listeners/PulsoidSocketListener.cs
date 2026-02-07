using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace HRtoVRChat.Listeners;

internal class PulsoidSocketListener : IHrListener {
    private Task? _task;
    private int HR;
    private string pubUrl = string.Empty;
    private CancellationTokenSource shouldUpdate = new();
    private readonly ILogger<PulsoidSocketListener> _logger;

    private WebsocketTemplate? wst;

    public PulsoidSocketListener(ILogger<PulsoidSocketListener> logger)
    {
        _logger = logger;
    }

    public bool Init(string url) {
        shouldUpdate = new CancellationTokenSource();
        pubUrl = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=" + url;
        StartThread();
        _logger.LogInformation("PulsoidSocketManager Initialized!");
        return true;
    }

    public void Stop() {
        shouldUpdate.Cancel();
        VerifyClosedThread();
    }

    public string Name => "Pulsoid";

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
        if (_task != null) {
            if (!_task.IsCompleted)
                Stop();
        }
    }

    private void StartThread() {
        VerifyClosedThread();
        var token = shouldUpdate.Token;
        _task = Task.Run(async () => {
            wst = new WebsocketTemplate(pubUrl, _logger);
            wst.OnMessage = (message) =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    // Parse HR
                    JObject? jo = null;
                    try
                    {
                        jo = JObject.Parse(message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to parse JObject!");
                    }

                    if (jo != null)
                    {
                        try
                        {
                            HR = jo["data"]?["heart_rate"]?.Value<int>() ?? 0;
                        }
                        catch (Exception)
                        {
                            _logger.LogError("Failed to parse Heart Rate!");
                        }
                    }
                }
            };

            await wst.Start();
            while (!token.IsCancellationRequested) {
                 try {
                     await Task.Delay(1000, token);
                 } catch (TaskCanceledException) { break; }
            }
        }, token);
    }
}
