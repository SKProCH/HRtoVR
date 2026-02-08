using System;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.TextFile;

internal class TextFileListener : IHrListener {
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private FileSystemWatcher? _watcher;
    private readonly ILogger<TextFileListener> _logger;
    private readonly IOptionsMonitor<TextFileOptions> _options;

    public TextFileListener(ILogger<TextFileListener> logger, IOptionsMonitor<TextFileOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public void Start() {
        if (string.IsNullOrEmpty(_options.CurrentValue.Location))
        {
            _logger.LogError("Text file location is not configured!");
            _isConnected.OnNext(false);
            return;
        }

        var fullPath = Path.GetFullPath(_options.CurrentValue.Location);
        var directory = Path.GetDirectoryName(fullPath);
        var fileName = Path.GetFileName(fullPath);

        if (directory == null || !Directory.Exists(directory))
        {
            _logger.LogError("Directory does not exist: {Directory}", directory);
            _isConnected.OnNext(false);
            return;
        }

        _ = UpdateHeartRateAsync().ConfigureAwait(false);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;

        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Started monitoring {File}", fullPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e) => _ = UpdateHeartRateAsync();
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _isConnected.OnNext(false);
        _heartRate.OnNext(0);
    }
    private void OnFileRenamed(object sender, RenamedEventArgs e) => _ = UpdateHeartRateAsync();

    public void Stop() {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Deleted -= OnFileDeleted;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Dispose();
            _watcher = null;
        }
        _isConnected.OnNext(false);
        _heartRate.OnNext(0);
    }

    public string Name => "TextFile";
    public object? Settings => _options.CurrentValue;
    public string? SettingsSectionName => "TextFileOptions";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;

    private async Task UpdateHeartRateAsync()
    {
        if (!File.Exists(_options.CurrentValue.Location))
        {
            _isConnected.OnNext(false);
            _heartRate.OnNext(0);
            return;
        }

        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var stream = new FileStream(_options.CurrentValue.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();

                if (int.TryParse(text.Trim(), out var hr))
                {
                    _heartRate.OnNext(hr);
                    _isConnected.OnNext(true);
                }
                else
                {
                    _logger.LogWarning("Failed to parse heart rate from text: {Text}", text);
                }
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(100);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reading text file");
                _isConnected.OnNext(false);
                break;
            }
        }
    }
}
