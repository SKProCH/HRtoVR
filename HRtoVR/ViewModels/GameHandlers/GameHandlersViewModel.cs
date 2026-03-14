using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Alias;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.ViewModels.Listeners;
using Material.Icons;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.ViewModels.GameHandlers;

using System.Reactive.Linq;
using HRtoVRChat.Models;
using ReactiveUI;

public class GameHandlersViewModel : ViewModelBase, IPageViewModel {
    public string Title => "Games";
    public MaterialIconKind Icon => MaterialIconKind.Gamepad;
    public ConnectionState? State { get; private set; }

    public SourceList<GameHandlerViewModel> Handlers { get; } = new();

    private readonly IServiceProvider _serviceProvider;

    public GameHandlersViewModel(
        IEnumerable<IGameHandler> gameHandlers,
        IServiceProvider serviceProvider,
        IOptionsManager<AppOptions> appOptionsManager) {
        _serviceProvider = serviceProvider;
        foreach (var handler in gameHandlers) {
            var handlerVm = new GameHandlerViewModel(handler, appOptionsManager);
            handlerVm.Settings = CreateSettingsViewModel(handler);

            Handlers.Add(handlerVm);
        }

        Handlers.Connect()
            .Filter(model => model.IsEnabled)
            .Maximum(x => (int)x.State)
            .Select(i => (ConnectionState)i)
            .BindTo(this, x => x.State);
    }

    private IListenerSettingsViewModel? CreateSettingsViewModel(IGameHandler handler) {
        var optionsTypeName = $"{handler.Name}Options";
        var optionsType = typeof(IGameHandler).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(optionsTypeName, StringComparison.OrdinalIgnoreCase));

        if (optionsType == null) return null;

        var managerType = typeof(IOptionsManager<>).MakeGenericType(optionsType);
        var optionsManager = _serviceProvider.GetService(managerType);

        if (optionsManager == null) return null;

        var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
        var currentValueProperty = monitorType.GetProperty("CurrentValue");
        var settings = currentValueProperty?.GetValue(optionsManager);

        return settings != null ? new ConfigSettingsViewModel(settings) : null;
    }
}