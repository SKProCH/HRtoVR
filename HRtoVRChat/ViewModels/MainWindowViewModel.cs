using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using HRtoVRChat.Services;
using HRtoVRChat.ViewModels.GameHandlers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ViewModelBase CurrentPage { get; set; }

    public ProgramViewModel ProgramVM { get; }
    public ListenersViewModel ListenersVM { get; }
    public GameHandlersViewModel GameHandlersVM { get; }
    public ConfigViewModel ConfigVM { get; }

    // Commands
    public ReactiveCommand<ViewModelBase, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    // Events
    public event Action? RequestHide;

    private readonly ITrayIconService _trayIconService;

    public MainWindowViewModel(
        ProgramViewModel programVM,
        ListenersViewModel listenersVM,
        GameHandlersViewModel gameHandlersVM,
        ConfigViewModel configVM,
        ITrayIconService trayIconService)
    {
        ProgramVM = programVM;
        ListenersVM = listenersVM;
        GameHandlersVM = gameHandlersVM;
        ConfigVM = configVM;
        _trayIconService = trayIconService;

        // Global Initialization
        if (!string.IsNullOrEmpty(App.LocalDirectory) && !Directory.Exists(App.LocalDirectory))
            Directory.CreateDirectory(App.LocalDirectory);

        // _configService.CreateConfig(); // Config is loaded via DI

        // Default Page
        CurrentPage = ProgramVM;

        // Commands
        SwitchPanelCommand = ReactiveCommand.Create<ViewModelBase>(vm =>
        {
            CurrentPage = vm;
        });

        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);

        HideAppCommand = ReactiveCommand.Create(() =>
        {
             _trayIconService.Update(new TrayIconInfo { HideApplication = true });
             RequestHide?.Invoke();
        });

        ExitAppCommand = ReactiveCommand.Create(() =>
        {
            Environment.Exit(0);
        });
    }
}
