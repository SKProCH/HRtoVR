using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ListenersViewModel : ViewModelBase {
    public ObservableCollection<ListenerViewModel> Listeners { get; } = new();
    [Reactive] public ListenerViewModel? SelectedListener { get; set; }

    private readonly IOptionsManager<AppOptions> _appOptions;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IHrListener> _hrListeners;

    public ListenersViewModel(IOptionsManager<AppOptions> appOptions, IConfiguration configuration, IEnumerable<IHrListener> hrListeners) {
        _appOptions = appOptions;
        _configuration = configuration;
        _hrListeners = hrListeners;

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
            if (listener.Settings != null && listener.SettingsSectionName != null) {
                AddSettings(listenerVM, listener.Settings, listener.SettingsSectionName);
            }

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

    private void AddSettings(ListenerViewModel listener, object configObject, string sectionName) {
        if (configObject == null) return;

        foreach (var prop in configObject.GetType().GetProperties()) {
            if (prop.CanRead && prop.CanWrite) {
                listener.Settings.Add(new ConfigItemViewModel(configObject, prop, $"{sectionName}:{prop.Name}",
                    _configuration));
            }
        }
    }
}