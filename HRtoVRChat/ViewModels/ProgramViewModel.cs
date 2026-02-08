using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HRtoVRChat.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;
using System.Reactive.Linq;

namespace HRtoVRChat.ViewModels;

public class ProgramViewModel : ViewModelBase
{
    [Reactive] public string StatusText { get; set; } = "STATUS: INITIALIZING";
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public string ActiveListenerName { get; set; } = "None";

    public ReactiveCommand<Unit, Unit> OpenArgumentsCommand { get; }

    private readonly IHRService _hrService;
    private readonly ITrayIconService _trayIconService;

    public ProgramViewModel(IHRService hrService, ITrayIconService trayIconService)
    {
        _hrService = hrService;
        _trayIconService = trayIconService;

        OpenArgumentsCommand = ReactiveCommand.Create(() => _trayIconService.ArgumentsWindow?.Show());

        _hrService.HeartRate
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(hr => HeartRate = hr);

        _hrService.IsConnected
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(connected => IsConnected = connected);

        _hrService.ActiveListener
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(listener => ActiveListenerName = listener?.Name ?? "None");

        _hrService.IsConnected.CombineLatest(_hrService.ActiveListener, (connected, listener) =>
            $"STATUS: {(listener != null ? (connected ? "CONNECTED" : "DISCONNECTED") : "STOPPED")}")
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status => StatusText = status);
    }
}
