using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    public ObservableCollection<ListenerViewModel> Listeners { get; } = new();
    public ObservableCollection<ConfigItemViewModel> GlobalSettings { get; } = new();

    [Reactive] public ListenerViewModel? SelectedListener { get; set; }

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
        this.WhenAnyValue(x => x.SelectedListener)
            .Subscribe(listener => {
                if (listener != null)
                {
                    var config = _appOptions.CurrentValue;
                    if (config.ActiveListener != listener.Id)
                    {
                        config.ActiveListener = listener.Id;
                        _configuration["HrType"] = listener.Id;
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
        Listeners.Clear();
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

        // 2. Load Listeners
        // Fitbit
        var fitbit = new ListenerViewModel("Fitbit", "fitbithrtows");
        AddSettings(fitbit, config.FitbitOptions, "FitbitOptions");
        Listeners.Add(fitbit);

        // HRProxy
        var hrproxy = new ListenerViewModel("HRProxy", "hrproxy");
        AddSettings(hrproxy, config.HRProxyOptions, "HRProxyOptions");
        Listeners.Add(hrproxy);

        // HypeRate
        var hyperate = new ListenerViewModel("HypeRate", "hyperate");
        AddSettings(hyperate, config.HypeRateOptions, "HypeRateOptions");
        Listeners.Add(hyperate);

        // Pulsoid
        var pulsoid = new ListenerViewModel("Pulsoid (Legacy)", "pulsoid");
        AddSettings(pulsoid, config.PulsoidOptions, "PulsoidOptions");
        Listeners.Add(pulsoid);

        // PulsoidSocket
        var pulsoidSocket = new ListenerViewModel("Pulsoid (Socket)", "pulsoidsocket");
        AddSettings(pulsoidSocket, config.PulsoidSocketOptions, "PulsoidSocketOptions");
        Listeners.Add(pulsoidSocket);

        // Stromno
        var stromno = new ListenerViewModel("Stromno", "stromno");
        AddSettings(stromno, config.StromnoOptions, "StromnoOptions");
        Listeners.Add(stromno);

        // TextFile
        var textfile = new ListenerViewModel("TextFile", "textfile");
        AddSettings(textfile, config.TextFileOptions, "TextFileOptions");
        Listeners.Add(textfile);

        // SDK
        var sdk = new ListenerViewModel("SDK", "sdk");
        // SDK has no specific config object in Config class, but maybe we can add a placeholder or nothing
        Listeners.Add(sdk);

        // Select the active listener
        SelectedListener = Listeners.FirstOrDefault(m => m.Id.Equals(config.ActiveListener, StringComparison.OrdinalIgnoreCase));
    }

    private void AddSettings(ListenerViewModel listener, object configObject, string sectionName)
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
                 listener.Settings.Add(new ConfigItemViewModel(configObject, prop, $"{sectionName}:{prop.Name}", _configuration));
             }
        }
    }
}
