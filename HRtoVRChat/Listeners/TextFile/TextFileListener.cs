using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.TextFile;

internal class TextFileListener : IHrListener {
    private Task? _task;
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
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
            _isConnected.OnNext(true);
            StartThread();
        }
        else
        {
            _logger.LogError("Failed to find text file!");
            _isConnected.OnNext(false);
        }
    }

    public void Stop() {
        shouldUpdate.Cancel();
        _isConnected.OnNext(false);
        _heartRate.OnNext(0);
        VerifyClosedThread();
    }

    public string Name => "TextFile";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

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
                    _isConnected.OnNext(false);
                }

                // cast to int
                if (!failed)
                {
                    try {
                        tempHR = Convert.ToInt32(text);
                        _isConnected.OnNext(true);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, "Failed to parse to int!");
                    }
                }

                _heartRate.OnNext(tempHR);
                try {
                    await Task.Delay(500, token);
                } catch (TaskCanceledException) { break; }
            }
        }, token);
    }
}
