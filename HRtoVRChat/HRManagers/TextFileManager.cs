using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace HRtoVRChat.HRManagers;

internal class TextFileManager : HRManager {
    private Thread? _thread;
    private int HR;
    private string pubFe = string.Empty;
    private CancellationTokenSource shouldUpdate = new();
    private readonly ILogger<TextFileManager> _logger;

    public TextFileManager(ILogger<TextFileManager> logger)
    {
        _logger = logger;
    }

    public bool Init(string fileLocation) {
        var fe = File.Exists(fileLocation);
        if (fe) {
            _logger.LogInformation("Found text file!");
            pubFe = fileLocation;
            shouldUpdate = new CancellationTokenSource();
            StartThread();
        }
        else
            _logger.LogError("Failed to find text file!");

        return fe;
    }

    public void Stop() {
        shouldUpdate.Cancel();
        VerifyClosedThread();
    }

    public string GetName() {
        return "TextFile";
    }

    public int GetHR() {
        return HR;
    }

    public bool IsOpen() {
        return !shouldUpdate.IsCancellationRequested && HR > 0;
    }

    public bool IsActive() {
        return !shouldUpdate.IsCancellationRequested;
    }

    private void VerifyClosedThread() {
        if (_thread != null) {
            if (_thread.IsAlive)
                shouldUpdate.Cancel();
        }
    }

    private void StartThread() {
        VerifyClosedThread();
        _thread = new Thread(() => {
            while (!shouldUpdate.IsCancellationRequested) {
                var failed = false;
                var tempHR = 0;
                // get text
                var text = string.Empty;
                try { text = File.ReadAllText(pubFe); }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to find Text File!");
                    failed = true;
                }

                // cast to int
                if (!failed)
                    try { tempHR = Convert.ToInt32(text); }
                    catch (Exception e) { _logger.LogError(e, "Failed to parse to int!"); }

                HR = tempHR;
                Thread.Sleep(500);
            }
        });
        _thread.Start();
    }
}
