using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using HRtoVRChat.ViewModels;
using ReactiveUI;

namespace HRtoVRChat.Services;

public interface ITrayIconService
{
    Window? MainWindow { get; set; }
    void Init(Application app);
    void Update(TrayIconInfo info);
}

public class TrayIconService : ITrayIconService
{
    public Window? MainWindow { get; set; }

    private readonly Dictionary<string, NativeMenuItemBase> _nativeMenuItems = new()
    {
        ["Status"] = new NativeMenuItem
        {
            Header = "Status: STOPPED",
            ToggleType = NativeMenuItemToggleType.None
        },
        ["-1"] = new NativeMenuItemSeparator(),
        ["HideApplication"] = new NativeMenuItem
        {
            Header = "Hide Application",
            ToggleType = NativeMenuItemToggleType.CheckBox
        },
        ["Exit"] = new NativeMenuItem
        {
            Header = "Exit",
            ToggleType = NativeMenuItemToggleType.None
        }
    };

    private readonly IHRService _hrService;

    public TrayIconService(IHRService hrService)
    {
        _hrService = hrService;
        // Wire up commands
        ((NativeMenuItem)_nativeMenuItems["HideApplication"]).Command = new TrayIconClicked(this, "HideApplication", "Hide Application");
        ((NativeMenuItem)_nativeMenuItems["Exit"]).Command = new TrayIconClicked(this, "Exit", "Exit");

        _hrService.IsConnected.CombineLatest(_hrService.ActiveListener, (connected, listener) =>
                $"{(listener != null ? (connected ? "CONNECTED" : "DISCONNECTED") : "STOPPED")}")
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(status => Update(new TrayIconInfo { Status = status }));
    }

    public void Init(Application app)
    {
        var nm = new NativeMenu();
        foreach (var (key, value) in _nativeMenuItems)
            nm.Add(value);

        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetTools.Icon),
            ToolTipText = "HRtoVRChat",
            Menu = nm
        };

        var ti = new TrayIcons();
        ti.Add(trayIcon);
        TrayIcon.SetIcons(app, ti);
    }

    public void Update(TrayIconInfo info)
    {
        foreach (var keyValuePair in _nativeMenuItems)
        {
            if (!keyValuePair.Key.Contains('-'))
            {
                var nativeMenuItem = (NativeMenuItem)keyValuePair.Value;
                switch (keyValuePair.Key)
                {
                    case "Status":
                        if (!string.IsNullOrEmpty(info.Status))
                            nativeMenuItem.Header = "Status: " + info.Status;
                        break;
                    case "HideApplication":
                        if (info.HideApplication != null)
                        {
                            nativeMenuItem.Header = info.HideApplication ?? false ? "✅ Hide Application" : "Hide Application";
                            nativeMenuItem.IsChecked = info.HideApplication ?? false;
                        }
                        break;
                }
            }
        }
    }

    private class TrayIconClicked : ICommand
    {
        private readonly TrayIconService _service;
        private readonly string _id;
        private readonly string _cachedHeader;

        public TrayIconClicked(TrayIconService service, string id, string cachedHeader)
        {
            _service = service;
            _id = id;
            _cachedHeader = cachedHeader;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (_service._nativeMenuItems.TryGetValue(_id, out var item) && item is NativeMenuItem nmi)
            {
                if (nmi.ToggleType == NativeMenuItemToggleType.CheckBox)
                {
                    nmi.IsChecked = !nmi.IsChecked;
                    nmi.Header = nmi.IsChecked ? "✅ " + _cachedHeader : _cachedHeader;
                }

                if (_service.MainWindow != null)
                {
                    switch (_id)
                    {
                        case "HideApplication":
                            if (nmi.IsChecked)
                                _service.MainWindow.Hide();
                            else
                                _service.MainWindow.Show();
                            break;
                        case "Exit":
                            if (_service.MainWindow.DataContext is MainWindowViewModel vmExit)
                                vmExit.ExitAppCommand.Execute(Unit.Default).Subscribe();
                            break;
                    }
                }
            }
        }

        public event EventHandler? CanExecuteChanged = (sender, args) => { };
    }
}
