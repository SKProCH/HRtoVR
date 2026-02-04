using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace HRtoVRChat;

public static class TrayIconManager {
    public static MainWindow? MainWindow;
    public static Arguments? ArgumentsWindow;

    public static Dictionary<string, NativeMenuItemBase> nativeMenuItems = new() {
        ["Status"] = new NativeMenuItem {
            Header = "Status: STOPPED",
            ToggleType = NativeMenuItemToggleType.None
        },
        ["-1"] = new NativeMenuItemSeparator(),
        ["AutoStart"] = new NativeMenuItem {
            Header = "Auto Start",
            ToggleType = NativeMenuItemToggleType.CheckBox,
            Command = new TrayIconClicked("AutoStart", "Auto Start")
        },
        ["SkipVRCCheck"] = new NativeMenuItem {
            Header = "Skip VRChat Check",
            ToggleType = NativeMenuItemToggleType.CheckBox,
            Command = new TrayIconClicked("SkipVRCCheck", "Skip VRChat Check")
        },
        ["NeosBridge"] = new NativeMenuItem {
            Header = "NeosBridge",
            ToggleType = NativeMenuItemToggleType.CheckBox,
            Command = new TrayIconClicked("NeosBridge", "Neos Bridge")
        },
        ["-2"] = new NativeMenuItemSeparator(),
        ["Start"] = new NativeMenuItem {
            Header = "Start",
            ToggleType = NativeMenuItemToggleType.None,
            Command = new TrayIconClicked("Start", "Start")
        },
        ["Stop"] = new NativeMenuItem {
            Header = "Stop",
            ToggleType = NativeMenuItemToggleType.None,
            Command = new TrayIconClicked("Stop", "Stop")
        },
        ["Kill"] = new NativeMenuItem {
            Header = "Kill all Processes",
            ToggleType = NativeMenuItemToggleType.None,
            Command = new TrayIconClicked("Kill", "Kill all Processes")
        },
        ["-3"] = new NativeMenuItemSeparator(),
        ["HideApplication"] = new NativeMenuItem {
            Header = "Hide Application",
            ToggleType = NativeMenuItemToggleType.CheckBox,
            Command = new TrayIconClicked("HideApplication", "Hide Application")
        },
        ["Exit"] = new NativeMenuItem {
            Header = "Exit",
            ToggleType = NativeMenuItemToggleType.None,
            Command = new TrayIconClicked("Exit", "Exit")
        }
    };

    public static void Init(AvaloniaObject o) {
        var nm = new NativeMenu();
        foreach (var (key, value) in nativeMenuItems)
            nm.Add(value);
        var trayIcon = new TrayIcon {
            Icon = new WindowIcon(AssetTools.Icon),
            ToolTipText = "HRtoVRChat",
            Menu = nm
        };
        var ti = new TrayIcons();
        ti.Add(trayIcon);
        TrayIcon.SetIcons(o, ti);
    }

    public static void Update(UpdateTrayIconInformation information) {
        foreach (var keyValuePair in nativeMenuItems) {
            if (!keyValuePair.Key.Contains('-')) {
                var nativeMenuItem = (NativeMenuItem)keyValuePair.Value;
                switch (keyValuePair.Key) {
                    case "Status":
                        if (!string.IsNullOrEmpty(information.Status))
                            nativeMenuItem.Header = "Status: " + information.Status;
                        break;
                    case "AutoStart":
                        if (information.AutoStart != null) {
                            var as_nmi = (NativeMenuItem)nativeMenuItems["AutoStart"];
                            as_nmi.Header =
                                information.AutoStart ?? false ? "✅ Auto Start" : "Auto Start";
                            as_nmi.IsChecked = information.AutoStart ?? false;
                        }

                        break;
                    case "SkipVRCCheck":
                        if (information.SkipVRCCheck != null) {
                            var svc_nmi = (NativeMenuItem)nativeMenuItems["SkipVRCCheck"];
                            svc_nmi.Header =
                                information.SkipVRCCheck ?? false ? "✅ Skip VRChat Check" : "Skip VRChat Check";
                            svc_nmi.IsChecked = information.SkipVRCCheck ?? false;
                        }

                        break;
                    case "NeosBridge":
                        if (information.NeosBridge != null) {
                            var svc_nmi = (NativeMenuItem)nativeMenuItems["NeosBridge"];
                            svc_nmi.Header =
                                information.NeosBridge ?? false ? "✅ Neos Bridge" : "Neos Bridge";
                            svc_nmi.IsChecked = information.NeosBridge ?? false;
                        }

                        break;
                    case "HideApplication":
                        if (information.HideApplication != null) {
                            var ha_nmi = (NativeMenuItem)nativeMenuItems["HideApplication"];
                            ha_nmi.Header =
                                information.HideApplication ?? false ? "✅ Hide Application" : "Hide Application";
                            ha_nmi.IsChecked = information.HideApplication ?? false;
                        }

                        break;
                }
            }
        }
    }

    public class UpdateTrayIconInformation {
        public bool? AutoStart;
        public bool? HideApplication;
        public bool? NeosBridge;
        public bool? SkipVRCCheck;
        public string Status = string.Empty;
    }

    private class TrayIconClicked : ICommand {
        private readonly string cachedHeader;
        private readonly string id;

        public TrayIconClicked(string id, string cachedHeader) {
            this.id = id;
            this.cachedHeader = cachedHeader;
        }

        public bool CanExecute(object? parameter) {
            return true;
        }

        public void Execute(object? parameter) {
            var nmi = (NativeMenuItem)nativeMenuItems[id];
            if (nmi.ToggleType == NativeMenuItemToggleType.CheckBox) {
                nmi.IsChecked = !nmi.IsChecked;
                if (nmi.IsChecked)
                    nmi.Header = "✅ " + cachedHeader;
                else
                    nmi.Header = cachedHeader;
            }

            if (MainWindow != null) {
                switch (id) {
                    case "AutoStart":
                        ArgumentsWindow.autostart.IsChecked = nmi.IsChecked;
                        ArgumentsWindow.AutoStartButtonPressed(null, null);
                        break;
                    case "SkipVRCCheck":
                        ArgumentsWindow.skipvrc.IsChecked = nmi.IsChecked;
                        ArgumentsWindow.SkipVRCCheckButtonPressed(null, null);
                        break;
                    case "NeosBridge":
                        ArgumentsWindow.neosbridge.IsChecked = nmi.IsChecked;
                        ArgumentsWindow.NeosBridgeButtonPressed(null, null);
                        break;
                    case "Start":
                        MainWindow.StartButtonPressed(null, null);
                        break;
                    case "Stop":
                        MainWindow.StopButtonPressed(null, null);
                        break;
                    case "Kill":
                        MainWindow.KillButtonPressed(null, null);
                        break;
                    case "HideApplication":
                        if (nmi.IsChecked)
                            MainWindow.Hide();
                        else
                            MainWindow.Show();
                        break;
                    case "Exit":
                        MainWindow.KillButtonPressed(null, null);
                        Environment.Exit(0);
                        break;
                }
            }
        }

        public event EventHandler? CanExecuteChanged = (sender, args) => { };
    }
}