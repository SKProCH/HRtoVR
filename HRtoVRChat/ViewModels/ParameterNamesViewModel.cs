using System;
using System.Collections.Generic;
using System.Reactive;
using Avalonia.Controls;
using HRtoVRChat.Configs;
using HRtoVRChat.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ParameterNamesViewModel : ViewModelBase
{
    public Dictionary<string, string> ParameterKeys { get; } = new();

    [Reactive] public string SelectedParameterKey { get; set; } = "";
    [Reactive] public string SelectedParameterName { get; set; } = "Select a Parameter";
    [Reactive] public string SelectedParameterType { get; set; } = "unknown";
    [Reactive] public string SelectedParameterDescription { get; set; } = "Description";
    [Reactive] public string ParameterValue { get; set; } = "";

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<string, Unit> SelectParameterCommand { get; }

    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IConfiguration _configuration;

    public ParameterNamesViewModel(IOptionsMonitor<AppOptions> appOptions, IConfiguration configuration)
    {
        _appOptions = appOptions;
        _configuration = configuration;

        // Load keys
        foreach (var prop in typeof(ParameterNamesOptions).GetProperties())
        {
            ParameterKeys.Add(prop.Name, prop.Name);
        }

        this.WhenAnyValue(x => x.SelectedParameterKey)
            .Subscribe(LoadParameterData);

        SaveCommand = ReactiveCommand.Create(Save);
        SelectParameterCommand = ReactiveCommand.Create<string>(key => SelectedParameterKey = key);
    }

    private void LoadParameterData(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        SelectedParameterName = key;

        // Load value from config
        var prop = typeof(ParameterNamesOptions).GetProperty(key);
        if (prop != null)
        {
            var value = prop.GetValue(_appOptions.CurrentValue.ParameterNames)?.ToString();
            ParameterValue = value ?? "";
        }

        // Load metadata
        if (ParameterData.ParameterDatas.TryGetValue(key, out var pd))
        {
            SelectedParameterType = pd.type;
            SelectedParameterDescription = pd.description;
        }
        else
        {
            // Fallback or error
             SelectedParameterType = "unknown";
             SelectedParameterDescription = "Description";
        }
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(SelectedParameterKey)) return;

        try
        {
            if (_configuration != null)
            {
                _configuration[$"ParameterNames:{SelectedParameterKey}"] = ParameterValue;
            }
        }
        catch (Exception)
        {
            MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "ParameterNames",
                ContentMessage = "Failed to save Parameter " + SelectedParameterKey + "!",
                WindowIcon = new WindowIcon(AssetTools.Icon),
                Icon = Icon.Error,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            }).Show();
        }
    }

    public class ParameterData
    {
        public static readonly Dictionary<string, ParameterData> ParameterDatas = new()
        {
            ["onesHR"] = new ParameterData
            {
                type = "int",
                description = "Ones spot in the Heart Rate reading; 12**3** *(legacy)*"
            },
            ["tensHR"] = new ParameterData
            {
                type = "int",
                description = "Tens spot in the Heart Rate reading; 1**2**3 *(legacy)*"
            },
            ["hundredsHR"] = new ParameterData
            {
                type = "int",
                description = "Hundreds spot in the Heart Rate reading; **1**23 *(legacy)*"
            },
            ["isHRConnected"] = new ParameterData
            {
                type = "bool",
                description = "Returns whether the device's connection is valid or not"
            },
            ["isHRActive"] = new ParameterData
            {
                type = "bool",
                description = "Returns whether the connection is valid or not"
            },
            ["isHRBeat"] = new ParameterData
            {
                type = "bool",
                description = "Estimation on when the heart is beating"
            },
            ["HRPercent"] = new ParameterData
            {
                type = "float",
                description = "Range of HR between the MinHR and MaxHR config value"
            },
            ["FullHRPercent"] = new ParameterData
            {
                type = "float",
                description = "Range of HR between the MinHR and the MaxHR config value, from -1 to 1"
            },
            ["HR"] = new ParameterData
            {
                type = "int",
                description = "Returns the raw HR, ranged from 0 - 255. *(required)*"
            }
        };

        public string description = "";

        public string type = "";
    }
}
