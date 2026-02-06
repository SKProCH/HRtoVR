using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using HRtoVRChat;

namespace HRtoVRChat.ViewModels;

public class SetupWizardViewModel : ViewModelBase
{
    public enum WizardPage
    {
        HrType,
        Endpoint
    }

    [Reactive] public WizardPage CurrentPage { get; set; } = WizardPage.HrType;

    // HR Types
    [Reactive] public string SelectedHrTypeKey { get; set; } = "";

    // Endpoint
    [Reactive] public bool IsThisDevice { get; set; } = true;
    [Reactive] public bool IsAnotherDevice { get; set; }
    [Reactive] public string Ip { get; set; } = "127.0.0.1";
    [Reactive] public string SendPort { get; set; } = "9000";
    [Reactive] public string ListenPort { get; set; } = "9001";

    // Commands
    public ReactiveCommand<string, Unit> SelectHrTypeCommand { get; }
    public ReactiveCommand<Unit, Unit> ContinueCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    // Events
    public event Action<HRTypeSelector, Action<List<HRTypeExtraInfo>>>? RequestShowExtraInfo;
    public event Action? RequestClose;

    // Data
    public Dictionary<string, HRTypeSelector> HrTypes { get; } = new()
    {
        ["fhr"] = new HRTypeSelector("fitbithrtows")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("fitbitURL", "The WebSocket to listen to data", "ws://localhost:8080/", typeof(string))
            }
        },
        ["hrp"] = new HRTypeSelector("hrproxy")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("hrproxyId", "The code to pull HRProxy Data from", "ABCD", typeof(string))
            }
        },
        ["hr"] = new HRTypeSelector("hyperate")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("hyperateSessionId", "The code to pull HypeRate Data from", "ABCD", typeof(string))
            }
        },
        ["ps"] = new HRTypeSelector("pulsoid")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("pulsoidwidget", "The widgetId to pull HeartRate Data from", "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                    typeof(string))
            }
        },
        ["pss"] = new HRTypeSelector("pulsoidsocket")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("pulsoidkey", "The key for the OAuth API to pull HeartRate Data from",
                    "https://github.com/200Tigersbloxed/HRtoVRChat_OSC/wiki/Upgrading-from-Pulsoid-to-PulsoidSocket",
                    typeof(string))
            }
        },
        ["sn"] = new HRTypeSelector("stromno")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("stromnowidget", "The widgetId to pull HeartRate Data from Stromno",
                    "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", typeof(string))
            }
        },
        ["tf"] = new HRTypeSelector("textfile")
        {
            ExtraInfos = new List<HRTypeExtraInfo>
            {
                new("textfilelocation", "The location of the text file to pull HeartRate Data from",
                    "/desktop/file.txt", typeof(string))
            }
        },
        ["oc"] = new HRTypeSelector("omnicept"),
        ["sdk"] = new HRTypeSelector("sdk")
    };


    public SetupWizardViewModel()
    {
        SelectHrTypeCommand = ReactiveCommand.Create<string>(key => SelectedHrTypeKey = key);
        ContinueCommand = ReactiveCommand.Create(Continue);
        BackCommand = ReactiveCommand.Create(Back);

        // Subscriptions
        this.WhenAnyValue(x => x.IsAnotherDevice)
            .Where(x => x)
            .Subscribe(_ => IsThisDevice = false);
    }

    private void Continue()
    {
        if (CurrentPage == WizardPage.HrType)
        {
            if (!string.IsNullOrEmpty(SelectedHrTypeKey) && HrTypes.ContainsKey(SelectedHrTypeKey))
            {
                var selector = HrTypes[SelectedHrTypeKey];

                // Show Extra Info if needed
                if (selector.ExtraInfos.Count > 0)
                {
                    RequestShowExtraInfo?.Invoke(selector, (vals) =>
                    {
                        // Apply values
                        ConfigManager.LoadedConfig.hrType = selector.Name;
                        foreach (var info in vals)
                        {
                            try
                            {
                                ConfigManager.LoadedConfig.GetType().GetField(info.name)?.SetValue(
                                    ConfigManager.LoadedConfig,
                                    Convert.ChangeType(info.AppliedValue, info.to));
                            }
                            catch { }
                        }
                        CurrentPage = WizardPage.Endpoint;
                    });
                }
                else
                {
                    ConfigManager.LoadedConfig.hrType = selector.Name;
                    CurrentPage = WizardPage.Endpoint;
                }
            }
            else
            {
                // Just move next if nothing selected? Original code did `else MoveNext(0)` which is weird if nothing selected.
                // But `selected` was initially null. If null, it did nothing.
                // If selected != null but not in dictionary (impossible?), it did MoveNext.
                // So we should probably require selection.
                // For now, let's assume user must select something.
            }
        }
        else if (CurrentPage == WizardPage.Endpoint)
        {
            if (IsAnotherDevice)
            {
                ConfigManager.LoadedConfig.ip = Ip;
                int.TryParse(SendPort, out var sp);
                ConfigManager.LoadedConfig.port = sp;
                int.TryParse(ListenPort, out var lp);
                ConfigManager.LoadedConfig.receiverPort = lp;
            }

            Finish();
        }
    }

    private void Back()
    {
        if (CurrentPage == WizardPage.Endpoint)
        {
            CurrentPage = WizardPage.HrType;
        }
    }

    private void Finish()
    {
        ConfigManager.SaveConfig(ConfigManager.LoadedConfig);

        MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
        {
            ButtonDefinitions = ButtonEnum.Ok,
            WindowIcon = new WindowIcon(AssetTools.Icon),
            Icon = Icon.Info,
            ContentTitle = "HRtoVRChat",
            ContentHeader = "Completed SetupWizard!",
            ContentMessage = "You can always visit the Config tab to change any of these settings again."
        }).Show();

        RequestClose?.Invoke();
    }
}
