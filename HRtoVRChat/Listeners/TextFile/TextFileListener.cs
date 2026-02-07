using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;
using HRtoVRChat.Configs;

namespace HRtoVRChat.Listeners;

internal class TextFileListener : IHrListener {
    private Task? _task;
    private int HR;
    private string pubFe = string.Empty;
    private CancellationTokenSource shouldUpdate = new();
    private readonly ILogger<TextFileListener> _logger;
    private readonly TextFileOptions _options;

    public TextFileListener(ILogger<TextFileListener> logger, IOptions<TextFileOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void Start() {
        var fe = File.Exists(_options.Location);
        if (fe) {
            _logger.LogInformation("Found text file!");
            pubFe = _options.Location;
            shouldUpdate = new CancellationTokenSource();
            StartThread();
        }
        else
            _logger.LogError("Failed to find text file!");
    }

    public void Stop() {
        shouldUpdate.Cancel();
        VerifyClosedThread();
    }

    public string Name => "TextFile";

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
        if (_task != null) {
            if (!_task.IsCompleted)
                shouldUpdate.Cancel();
        }
    }

    private void StartThread() {
        VerifyClosedThread();
        var token = shouldUpdate.Token;
        _task = Task.Run(async () => {
            while (!token.IsCancellationRequested) {
                var failed = false;
                var tempHR = 0;
                // get text
                var text = string.Empty;
                try { text = await File.ReadAllTextAsync(pubFe, token); }
                catch (Exception e) {
                    _logger.LogError(e, "Failed to find Text File!");
                    failed = true;
                }

                // cast to int
                if (!failed)
                    try { tempHR = Convert.ToInt32(text); }
                    catch (Exception e) { _logger.LogError(e, "Failed to parse to int!"); }

                HR = tempHR;
                try {
                    await Task.Delay(500, token);
                } catch (TaskCanceledException) { break; }
            }
        }, token);
    }
}
