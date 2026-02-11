using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.Listeners.Ble;
using HRtoVRChat.ViewModels.Listeners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ListenersViewModel : ViewModelBase {
    public ObservableCollection<ListenerViewModel> Listeners { get; } = new();
    [Reactive] public ListenerViewModel? SelectedListener { get; set; }

    private readonly IOptionsManager<AppOptions> _appOptions;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IHrListener> _hrListeners;
    private readonly IServiceProvider _serviceProvider;

    public ListenersViewModel(IOptionsManager<AppOptions> appOptions, IConfiguration configuration, IEnumerable<IHrListener> hrListeners, IServiceProvider serviceProvider) {
        _appOptions = appOptions;
        _configuration = configuration;
        _hrListeners = hrListeners;
        _serviceProvider = serviceProvider;

        LoadListeners();

        // Handle selection changes
        this.WhenAnyValue(x => x.SelectedListener)
            .Subscribe(listener => {
                if (listener != null) {
                    // Expand the selected listener and collapse others
                    foreach (var l in Listeners) {
                        l.IsExpanded = l == listener;
                    }

                    _appOptions.CurrentValue.ActiveListener = listener.Name;
                    _appOptions.Save();
                }
            });
    }

    private void LoadListeners() {
        Listeners.Clear();
        var config = _appOptions.CurrentValue;

        // Load Listeners from IHrListener instances
        foreach (var listener in _hrListeners) {
            var listenerVM = new ListenerViewModel(listener.Name);

            // Map listener to its settings
            listenerVM.Settings = CreateSettingsViewModel(listener);

            // Sync listener state to VM
            listener.HeartRate.Subscribe(hr => listenerVM.HeartRate = hr);
            listener.IsConnected.Subscribe(connected => listenerVM.IsConnected = connected);

            Listeners.Add(listenerVM);
        }

        // Select the active listener
        SelectedListener =
            Listeners.FirstOrDefault(m => m.Name.Equals(config.ActiveListener, StringComparison.OrdinalIgnoreCase));

        // Sync manual expansion with selection
        foreach (var listener in Listeners) {
            listener.WhenAnyValue(x => x.IsExpanded)
                .Subscribe(isExpanded => {
                    if (isExpanded) {
                        SelectedListener = listener;
                    }
                });
        }
    }

    private IListenerSettingsViewModel? CreateSettingsViewModel(IHrListener listener)
    {
        if (listener.SettingsViewModelType != null)
        {
            return _serviceProvider.GetRequiredService(listener.SettingsViewModelType) as IListenerSettingsViewModel;
        }

        var optionsTypeName = $"{listener.Name}Options";
        var optionsType = typeof(IHrListener).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(optionsTypeName, StringComparison.OrdinalIgnoreCase));

        if (optionsType == null) return null;

        var managerType = typeof(IOptionsManager<>).MakeGenericType(optionsType);
        var optionsManager = _serviceProvider.GetService(managerType);

        if (optionsManager == null) return null;

        // CurrentValue is defined in IOptionsMonitor<T>, which IOptionsManager<T> inherits from.
        // Interface reflection doesn't automatically find properties from base interfaces.
        var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
        var currentValueProperty = monitorType.GetProperty("CurrentValue");
        var settings = currentValueProperty?.GetValue(optionsManager);

        return settings != null ? new ConfigSettingsViewModel(settings) : null;
    }
}