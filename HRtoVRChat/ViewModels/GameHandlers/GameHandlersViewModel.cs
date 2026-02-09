using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.Services;
using HRtoVRChat.ViewModels.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels.GameHandlers;

public class GameHandlersViewModel : ViewModelBase, IDisposable
{
    public ObservableCollection<GameHandlerViewModel> Handlers { get; } = new();

    private readonly IEnumerable<IGameHandler> _gameHandlers;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHRService _hrService;
    private readonly IOptionsMonitor<VRChatOSCOptions> _vrcOptions;
    private readonly CompositeDisposable _disposables = new();

    public GameHandlersViewModel(
        IEnumerable<IGameHandler> gameHandlers,
        IServiceProvider serviceProvider,
        IHRService hrService,
        IOptionsMonitor<VRChatOSCOptions> vrcOptions)
    {
        _gameHandlers = gameHandlers;
        _serviceProvider = serviceProvider;
        _hrService = hrService;
        _vrcOptions = vrcOptions;

        LoadHandlers();

        // Update running status periodically
        Observable.Interval(TimeSpan.FromSeconds(2))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateRunningStatus())
            .DisposeWith(_disposables);
    }

    private void LoadHandlers()
    {
        Handlers.Clear();
        var appOptionsManager = _serviceProvider.GetRequiredService<IOptionsManager<AppOptions>>();

        foreach (var handler in _gameHandlers)
        {
            var handlerVM = new GameHandlerViewModel(handler.Name);
            handlerVM.Settings = CreateSettingsViewModel(handler);

            // Sync IsConnected from handler (integration status)
            handler.WhenAnyValue(x => x.IsConnected)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(isConnected => handlerVM.IsConnected = isConnected)
                .DisposeWith(_disposables);

            // Global HeartRate status
            _hrService.HeartRate
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(hr => handlerVM.HeartRate = hr)
                .DisposeWith(_disposables);

            // Sync IsEnabled with centralized dictionary in AppOptions
            if (appOptionsManager.CurrentValue.GameHandlers.TryGetValue(handler.Name, out var isEnabled))
            {
                handlerVM.IsEnabled = isEnabled;
            }

            handlerVM.WhenAnyValue(x => x.IsEnabled)
                .Subscribe(enabled =>
                {
                    var currentOptions = appOptionsManager.CurrentValue;
                    if (!currentOptions.GameHandlers.TryGetValue(handler.Name, out var currentVal) || currentVal != enabled)
                    {
                        currentOptions.GameHandlers[handler.Name] = enabled;
                        appOptionsManager.Save();
                    }
                })
                .DisposeWith(_disposables);

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

    public void UpdateRunningStatus()
    {
        foreach (var handlerVM in Handlers)
        {
            bool processRunning = false;
            if (handlerVM.Name == "VRChatOSC")
            {
                var vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
                var cvrRunning = _vrcOptions.CurrentValue.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
                processRunning = vrcRunning || cvrRunning;
            }
            else if (handlerVM.Name == "Neos")
            {
                processRunning = Process.GetProcessesByName("Neos").Length > 0;
            }

            handlerVM.IsRunning = processRunning;
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
