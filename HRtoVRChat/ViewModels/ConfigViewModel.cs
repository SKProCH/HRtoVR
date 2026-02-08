using System;
using System.Collections.Generic;
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
    public ObservableCollection<ConfigItemViewModel> GlobalSettings { get; } = new();

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
    }

    private void Initialize()
    {
        LoadConfigItems();
    }

    private void LoadConfigItems()
    {
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
    }
}
