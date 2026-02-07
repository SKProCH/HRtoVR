using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using HRtoVRChat.Configs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tommy.Serializer;

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

    public ConfigViewModel()
    {
        SaveConfigCommand = ReactiveCommand.Create(() => {
             ConfigManager.SaveConfig(ConfigManager.LoadedConfig);
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
                    var config = ConfigManager.LoadedConfig;
                    if (config.hrType != manager.Id)
                    {
                        config.hrType = manager.Id;
                        ConfigManager.SaveConfig(config);
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

        var config = ConfigManager.LoadedConfig;

        // 1. Load Global Settings
        // We manually select fields that are global
        var globalFields = new[] { "ip", "port", "receiverPort", "MaxHR", "MinHR", "ExpandCVR" };
        foreach (var fieldName in globalFields)
        {
            var field = config.GetType().GetField(fieldName);
            if (field != null)
            {
                GlobalSettings.Add(new ConfigItemViewModel(config, field));
            }
        }

        // 2. Load Managers
        // Fitbit
        var fitbit = new ManagerViewModel("Fitbit", "fitbithrtows");
        AddSettings(fitbit, config.FitbitConfig);
        Managers.Add(fitbit);

        // HRProxy
        var hrproxy = new ManagerViewModel("HRProxy", "hrproxy");
        AddSettings(hrproxy, config.HRProxyConfig);
        Managers.Add(hrproxy);

        // HypeRate
        var hyperate = new ManagerViewModel("HypeRate", "hyperate");
        AddSettings(hyperate, config.HypeRateConfig);
        Managers.Add(hyperate);

        // Pulsoid
        var pulsoid = new ManagerViewModel("Pulsoid (Legacy)", "pulsoid");
        AddSettings(pulsoid, config.PulsoidConfig);
        Managers.Add(pulsoid);

        // PulsoidSocket
        var pulsoidSocket = new ManagerViewModel("Pulsoid (Socket)", "pulsoidsocket");
        AddSettings(pulsoidSocket, config.PulsoidSocketConfig);
        Managers.Add(pulsoidSocket);

        // Stromno
        var stromno = new ManagerViewModel("Stromno", "stromno");
        AddSettings(stromno, config.StromnoConfig);
        Managers.Add(stromno);

        // TextFile
        var textfile = new ManagerViewModel("TextFile", "textfile");
        AddSettings(textfile, config.TextFileConfig);
        Managers.Add(textfile);

        // SDK
        var sdk = new ManagerViewModel("SDK", "sdk");
        // SDK has no specific config object in Config class, but maybe we can add a placeholder or nothing
        Managers.Add(sdk);

        // Select the active manager
        SelectedManager = Managers.FirstOrDefault(m => m.Id.Equals(config.hrType, StringComparison.OrdinalIgnoreCase));
    }

    private void AddSettings(ManagerViewModel manager, object configObject)
    {
        if (configObject == null) return;

        foreach (var field in configObject.GetType().GetFields())
        {
             // Check for TommyInclude or TommyComment if needed, but usually we want all public fields in config objects
             // The original code checked for TommyComment or just assumed fields.
             // Config fields in Config.cs have [TommyInclude].

             // Check if it should be included
             if (Attribute.IsDefined(field, typeof(TommyInclude)) || Attribute.IsDefined(field, typeof(TommyComment)))
             {
                 manager.Settings.Add(new ConfigItemViewModel(configObject, field));
             }
        }
    }
}
