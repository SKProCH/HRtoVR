using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    public ObservableCollection<ManagerViewModel> Managers { get; } = new();
    public ObservableCollection<ConfigItemViewModel> GlobalSettings { get; } = new();

    [Reactive] public ManagerViewModel? SelectedManager { get; set; }

    // Kept for global settings editing
    [Reactive] public string ConfigValueInput { get; set; } = "";

    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenParameterNamesCommand { get; }

    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IConfiguration _configuration;

    public ConfigViewModel(IOptionsMonitor<AppOptions> appOptions, IConfiguration configuration)
    {
        _appOptions = appOptions;
        _configuration = configuration;

        SaveConfigCommand = ReactiveCommand.Create(() => {
             // Config is auto-saved now via IConfiguration
        });

        OpenParameterNamesCommand = ReactiveCommand.Create(() => {
            if (!ParameterNames.IsOpen)
                new ParameterNames().Show();
        });

        Initialize();

        // Handle selection changes
        this.WhenAnyValue(x => x.SelectedManager)
            .Subscribe(manager => {
                if (manager != null)
                {
                    var config = _appOptions.CurrentValue;
                    if (config.ActiveListener != manager.Id)
                    {
                        config.ActiveListener = manager.Id;
                        _configuration["HrType"] = manager.Id;
                    }
                }
            });
    }

    private void Initialize()
    {
        LoadConfigItems();
    }

    private void LoadConfigItems()
    {
        Managers.Clear();
        GlobalSettings.Clear();

        var config = _appOptions.CurrentValue;

        // 1. Load Global Settings
        // We manually select fields that are global
        var globalFields = new[] { "Ip", "Port", "ReceiverPort", "MaxHR", "MinHR", "ExpandCVR" };
        foreach (var fieldName in globalFields)
        {
            var prop = config.GetType().GetProperty(fieldName);
            if (prop != null)
            {
                GlobalSettings.Add(new ConfigItemViewModel(config, prop, fieldName, _configuration));
            }
        }

        // 2. Load Managers
        // Fitbit
        var fitbit = new ManagerViewModel("Fitbit", "fitbithrtows");
        AddSettings(fitbit, config.FitbitOptions, "FitbitOptions");
        Managers.Add(fitbit);

        // HRProxy
        var hrproxy = new ManagerViewModel("HRProxy", "hrproxy");
        AddSettings(hrproxy, config.HRProxyOptions, "HRProxyOptions");
        Managers.Add(hrproxy);

        // HypeRate
        var hyperate = new ManagerViewModel("HypeRate", "hyperate");
        AddSettings(hyperate, config.HypeRateOptions, "HypeRateOptions");
        Managers.Add(hyperate);

        // Pulsoid
        var pulsoid = new ManagerViewModel("Pulsoid (Legacy)", "pulsoid");
        AddSettings(pulsoid, config.PulsoidOptions, "PulsoidOptions");
        Managers.Add(pulsoid);

        // PulsoidSocket
        var pulsoidSocket = new ManagerViewModel("Pulsoid (Socket)", "pulsoidsocket");
        AddSettings(pulsoidSocket, config.PulsoidSocketOptions, "PulsoidSocketOptions");
        Managers.Add(pulsoidSocket);

        // Stromno
        var stromno = new ManagerViewModel("Stromno", "stromno");
        AddSettings(stromno, config.StromnoOptions, "StromnoOptions");
        Managers.Add(stromno);

        // TextFile
        var textfile = new ManagerViewModel("TextFile", "textfile");
        AddSettings(textfile, config.TextFileOptions, "TextFileOptions");
        Managers.Add(textfile);

        // SDK
        var sdk = new ManagerViewModel("SDK", "sdk");
        // SDK has no specific config object in Config class, but maybe we can add a placeholder or nothing
        Managers.Add(sdk);

        // Select the active manager
        SelectedManager = Managers.FirstOrDefault(m => m.Id.Equals(config.ActiveListener, StringComparison.OrdinalIgnoreCase));
    }

    private void AddSettings(ManagerViewModel manager, object configObject, string sectionName)
    {
        if (configObject == null) return;

        foreach (var prop in configObject.GetType().GetProperties())
        {
             // Check for TommyInclude or TommyComment if needed, but usually we want all public fields in config objects
             // The original code checked for TommyComment or just assumed fields.
             // Config fields in Config.cs have [TommyInclude].

             // Check if it should be included
             // Since we moved to properties, we assume all properties are config items unless specified otherwise
             // Or we can check if it's read/write
             if (prop.CanRead && prop.CanWrite)
             {
                 manager.Settings.Add(new ConfigItemViewModel(configObject, prop, $"{sectionName}:{prop.Name}", _configuration));
             }
        }
    }
}
