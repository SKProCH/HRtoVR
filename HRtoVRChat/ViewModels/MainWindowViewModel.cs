using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
using HRtoVRChat.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ViewModelBase CurrentPage { get; set; }

    public HomeViewModel HomeVM { get; }
    public ProgramViewModel ProgramVM { get; }
    public ConfigViewModel ConfigVM { get; }

    // Commands
    public ReactiveCommand<ViewModelBase, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    // Events
    public event Action? RequestHide;

    private readonly ITrayIconService _trayIconService;
    private readonly ISoftwareService _softwareService;

    public MainWindowViewModel(
        HomeViewModel homeVM,
        ProgramViewModel programVM,
        ConfigViewModel configVM,
        ITrayIconService trayIconService,
        ISoftwareService softwareService)
    {
        HomeVM = homeVM;
        ProgramVM = programVM;
        ConfigVM = configVM;
        _trayIconService = trayIconService;
        _softwareService = softwareService;

        // Global Initialization
        if (!string.IsNullOrEmpty(_softwareService.LocalDirectory) && !Directory.Exists(_softwareService.LocalDirectory))
            Directory.CreateDirectory(_softwareService.LocalDirectory);

        // _configService.CreateConfig(); // Config is loaded via DI

        // Default Page
        CurrentPage = HomeVM;

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
            // Stop software if running
            _softwareService.StopSoftware();
            try {
                foreach (var process in Process.GetProcessesByName("HRtoVRChat")) {
                    process.Kill();
                }
            }
            catch (Exception) { }
            Environment.Exit(0);
        });
    }
}
