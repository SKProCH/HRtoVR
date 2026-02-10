using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.ViewModels.Listeners;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.ViewModels.GameHandlers;

public class GameHandlersViewModel : ViewModelBase
{
    public ObservableCollection<GameHandlerViewModel> Handlers { get; } = new();

    private readonly IServiceProvider _serviceProvider;

    public GameHandlersViewModel(
        IEnumerable<IGameHandler> gameHandlers,
        IServiceProvider serviceProvider,
        IOptionsManager<AppOptions> appOptionsManager)
    {
        _serviceProvider = serviceProvider;
        foreach (var handler in gameHandlers)
        {
            var handlerVM = new GameHandlerViewModel(handler, appOptionsManager);
            handlerVM.Settings = CreateSettingsViewModel(handler);

            Handlers.Add(handlerVM);
        }
    }

    private IListenerSettingsViewModel? CreateSettingsViewModel(IGameHandler handler)
    {
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
