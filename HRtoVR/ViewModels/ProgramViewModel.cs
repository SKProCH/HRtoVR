using System;
using System.Reactive.Linq;
using HRtoVR.Models;
using HRtoVR.Services;
using Material.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels;

public class ProgramViewModel : ViewModelBase, IPageViewModel {
    public string Title => "Program";
    public MaterialIconKind Icon => MaterialIconKind.Application;
    public ConnectionState? State => null;
    [Reactive] public string StatusText { get; set; } = "STATUS: INITIALIZING";
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public string ActiveListenerName { get; set; } = "None";

    public ProgramViewModel(IHRService hrService) {
        hrService.HeartRate
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(hr => HeartRate = hr);

        hrService.IsConnected
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(connected => IsConnected = connected);

        hrService.ActiveListener
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(listener => ActiveListenerName = listener?.Name ?? "None");

        hrService.IsConnected.CombineLatest(hrService.ActiveListener, (connected, listener) =>
                $"STATUS: {(listener != null ? connected ? "CONNECTED" : "DISCONNECTED" : "STOPPED")}")
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status => StatusText = status);
    }
}
