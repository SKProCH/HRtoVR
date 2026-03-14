using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using HRtoVR.Configs;
using HRtoVR.GameHandlers;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Models;
using HRtoVR.ViewModels.Listeners;
using Material.Icons;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace HRtoVR.ViewModels.GameHandlers;

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