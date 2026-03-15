using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Models;
using HRtoVR.ViewModels.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels;

public class ListenerViewModel : ViewModelBase {
    [Reactive] public bool IsExpanded { get; set; }
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public ConnectionState State { get; set; }
    [Reactive] public string Name { get; set; }
    public IListenerSettingsViewModel? Settings { get; }
    public IHrListener Listener { get; }
    public ICommand? RestartCommand { get; }

    public ListenerViewModel(IHrListener listener, IServiceProvider serviceProvider) {
        Settings = CreateSettingsViewModel(listener, serviceProvider);
        Listener = listener;
        Name = listener.Name;
        RestartCommand = ReactiveCommand.CreateFromTask(async () => {
            await listener.Stop();
            await listener.Start();
        });

        // Sync listener state to VM
        listener.HeartRate.BindTo(this, model => model.HeartRate);
        listener.IsConnected.BindTo(this, model => model.IsConnected);
        this.WhenAnyValue(model => model.IsConnected, model => model.HeartRate)
            .Select(tuple => ConnectionState.FromListenerState(tuple.Item1, tuple.Item2))
            .BindTo(this, model => model.State);
    }

    private IListenerSettingsViewModel?
        CreateSettingsViewModel(IHrListener listener, IServiceProvider serviceProvider) {
        if (listener.SettingsViewModelType != null) {
            return serviceProvider.GetRequiredService(listener.SettingsViewModelType) as IListenerSettingsViewModel;
        }

        var optionsTypeName = $"{listener.Name}Options";
        var optionsType = typeof(IHrListener).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(optionsTypeName, StringComparison.OrdinalIgnoreCase));

        if (optionsType == null) return null;

        var managerType = typeof(IOptionsManager<>).MakeGenericType(optionsType);
        var optionsManager = serviceProvider.GetService(managerType);

        if (optionsManager == null) return null;

        // CurrentValue is defined in IOptionsMonitor<T>, which IOptionsManager<T> inherits from.
        // Interface reflection doesn't automatically find properties from base interfaces.
        var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(optionsType);
        var currentValueProperty = monitorType.GetProperty("CurrentValue");
        var settings = currentValueProperty?.GetValue(optionsManager);

        return settings != null ? new ConfigSettingsViewModel(settings) : null;
    }
}