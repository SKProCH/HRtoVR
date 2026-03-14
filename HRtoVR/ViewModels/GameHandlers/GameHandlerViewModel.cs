using System;
using HRtoVR.Configs;
using HRtoVR.GameHandlers;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Models;
using HRtoVR.ViewModels.Listeners;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels.GameHandlers;

public class GameHandlerViewModel : ViewModelBase {
    public string Name { get; }
    public IGameHandler Handler { get; }
    [Reactive] public bool IsEnabled { get; set; } = true;
    [Reactive] public IListenerSettingsViewModel? Settings { get; set; }
    [Reactive] public ConnectionState State { get; set; }

    public GameHandlerViewModel(IGameHandler handler, IOptionsManager<AppOptions> appOptionsManager) {
        Handler = handler;
        Name = handler.Name;
        if (appOptionsManager.CurrentValue.GameHandlers.TryGetValue(handler.Name, out var enabled)) {
            IsEnabled = enabled;
        }

        this.WhenAnyValue(x => x.IsEnabled)
            .Subscribe(nowEnabled => {
                var currentOptions = appOptionsManager.CurrentValue;
                currentOptions.GameHandlers[handler.Name] = nowEnabled;
                appOptionsManager.Save();
            });

        this.WhenAnyValue(x => x.Handler.IsConnected)
            .Subscribe(connected => State = connected ? ConnectionState.Active : ConnectionState.Disconnected);
    }
}