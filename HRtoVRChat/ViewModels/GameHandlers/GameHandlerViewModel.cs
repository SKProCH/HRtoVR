using System;
using System.Reactive.Disposables;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.ViewModels.Listeners;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels.GameHandlers;

public class GameHandlerViewModel : ViewModelBase {
    public string Name { get; }
    public IGameHandler Handler { get; }
    [Reactive] public bool IsEnabled { get; set; } = true;
    [Reactive] public IListenerSettingsViewModel? Settings { get; set; }

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
    }
}