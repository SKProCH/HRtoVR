using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Serilog.Core;
using Serilog.Events;

namespace HRtoVR.Infrastructure.Logging;

public class LogMessage {
    public DateTimeOffset Timestamp { get; set; }
    public LogEventLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

public class LogSink : ILogEventSink {
    private readonly IFormatProvider? _formatProvider;
    private readonly int _maxEntries;
    public ObservableCollection<LogMessage> Logs { get; } = new();

    public LogSink(IFormatProvider? formatProvider = null, int maxEntries = 500) {
        _formatProvider = formatProvider;
        _maxEntries = maxEntries;
    }

    public void Emit(LogEvent logEvent) {
        var logMessage = new LogMessage {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level,
            Message = logEvent.RenderMessage(_formatProvider),
            Exception = logEvent.Exception?.ToString()
        };

        Dispatcher.UIThread.Post(() => {
            Logs.Add(logMessage);
            while (Logs.Count > _maxEntries) {
                Logs.RemoveAt(0);
            }
        });
    }

    public void Clear() {
        Dispatcher.UIThread.Post(() => Logs.Clear());
    }
}