using System;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using HRtoVR.Infrastructure.Logging;
using HRtoVR.ViewModels;
using TextMateSharp.Grammars;

namespace HRtoVR.Views;

public partial class LogsView : UserControl {
    private TextEditor _logEditor;
    private TextMate.Installation _textMateInstallation;

    public LogsView() {
        InitializeComponent();
        _logEditor = this.FindControl<TextEditor>("LogEditor")!;

        _logEditor.Options.AllowScrollBelowDocument = false;
        var registryOptions = new LogRegistryOptions(ThemeName.DarkPlus);
        _textMateInstallation = _logEditor.InstallTextMate(registryOptions);
        _textMateInstallation.SetGrammar("source.log");


        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnDataContextChanged(object? sender, EventArgs e) {
        if (DataContext is LogsViewModel vm) {
            vm.FilteredLogs.CollectionChanged -= OnLogsCollectionChanged;
            vm.FilteredLogs.CollectionChanged += OnLogsCollectionChanged;

            // Initial load
            RenderLogs(vm);
        }
    }

    private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (DataContext is not LogsViewModel vm) return;

        if (e.Action == NotifyCollectionChangedAction.Reset) {
            Dispatcher.UIThread.Post(() => _logEditor.Document.Text = string.Empty);
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null) {
            Dispatcher.UIThread.Post(() => {
                foreach (LogMessage log in e.NewItems) {
                    var text = FormatLogMessage(log);
                    _logEditor.AppendText(text);
                }

                Dispatcher.UIThread.Post(() => {
                    _logEditor.ScrollToEnd();
                });
            });
        }
    }

    private void RenderLogs(LogsViewModel vm) {
        var sb = new StringBuilder();
        foreach (var log in vm.FilteredLogs) {
            sb.Append(FormatLogMessage(log));
        }

        Dispatcher.UIThread.Post(() => {
            _logEditor.Document = new TextDocument(sb.ToString());
            Dispatcher.UIThread.Post(() => {
                _logEditor.ScrollToLine(_logEditor.Document.LineCount);
            }, DispatcherPriority.Background);
        });
    }

    private string FormatLogMessage(LogMessage log) {
        var sb = new StringBuilder();
        sb.Append($"[{log.Timestamp:HH:mm:ss.fff}] [{log.Level}] {log.Message}\n");
        if (!string.IsNullOrEmpty(log.Exception)) {
            sb.Append($"{log.Exception}\n");
        }

        return sb.ToString();
    }
}