using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using HRtoVR.Infrastructure.Logging;
using HRtoVR.Models;
using Material.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog.Events;

namespace HRtoVR.ViewModels;

public class LogsViewModel : ViewModelBase, IPageViewModel {
    public string Title => "Logs";
    public MaterialIconKind Icon => MaterialIconKind.FileDocument;
    public ConnectionState? State => null;
    private readonly LogSink _logSink;

    [Reactive] public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Debug;

    [Reactive] public ObservableCollection<LogMessage> FilteredLogs { get; set; }

    public LogsViewModel(LogSink logSink) {
        _logSink = logSink;
        FilteredLogs = new ObservableCollection<LogMessage>(_logSink.Logs.Where(l => l.Level >= MinimumLevel));

        this.WhenAnyValue(x => x.MinimumLevel)
            .Subscribe(_ => UpdateFilteredLogs());

        _logSink.Logs.CollectionChanged += (sender, args) => {
            if (args.NewItems != null) {
                foreach (LogMessage newLog in args.NewItems) {
                    if (newLog.Level >= MinimumLevel) {
                        FilteredLogs.Add(newLog);
                    }
                }
            }

            if (args.OldItems != null) {
                foreach (LogMessage oldLog in args.OldItems) {
                    FilteredLogs.Remove(oldLog);
                }
            }
        };

        ClearCommand = ReactiveCommand.Create(() => {
            _logSink.Clear();
            FilteredLogs.Clear();
        });
    }

    public ReactiveCommand<Unit, Unit> ClearCommand { get; }

    public LogEventLevel[] LogLevels { get; } = Enum.GetValues<LogEventLevel>();

    private void UpdateFilteredLogs() {
        FilteredLogs = new ObservableCollection<LogMessage>(_logSink.Logs.Where(l => l.Level >= MinimumLevel));
    }
}