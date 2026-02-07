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
    public UpdatesViewModel UpdatesVM { get; }
    public ConfigViewModel ConfigVM { get; }
    public IncomingDataViewModel IncomingDataVM { get; }

    // Commands
    public ReactiveCommand<string, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    // Events
    public event Action? RequestHide;

    private readonly ITrayIconService _trayIconService;
    private readonly ISoftwareService _softwareService;
    private readonly IBrowserService _browserService;

    public MainWindowViewModel(
        HomeViewModel homeVM,
        ProgramViewModel programVM,
        UpdatesViewModel updatesVM,
        ConfigViewModel configVM,
        IncomingDataViewModel incomingDataVM,
        ITrayIconService trayIconService,
        ISoftwareService softwareService,
        IBrowserService browserService)
    {
        HomeVM = homeVM;
        ProgramVM = programVM;
        UpdatesVM = updatesVM;
        ConfigVM = configVM;
        IncomingDataVM = incomingDataVM;
        _trayIconService = trayIconService;
        _softwareService = softwareService;
        _browserService = browserService;

        // Global Initialization
        if (!string.IsNullOrEmpty(_softwareService.LocalDirectory) && !Directory.Exists(_softwareService.LocalDirectory))
            Directory.CreateDirectory(_softwareService.LocalDirectory);

        // _configService.CreateConfig(); // Config is loaded via DI

        // Default Page
        CurrentPage = HomeVM;

        // Commands
        SwitchPanelCommand = ReactiveCommand.Create<string>(panel =>
        {
            CurrentPage = panel switch
            {
                "Home" => HomeVM,
                "Program" => ProgramVM,
                "Updates" => UpdatesVM,
                "Config" => ConfigVM,
                "IncomingData" => IncomingDataVM,
                _ => HomeVM
            };
        });

        OpenUrlCommand = ReactiveCommand.Create<string>(_browserService.OpenUrl);

        HideAppCommand = ReactiveCommand.Create(() =>
        {
             _trayIconService.Update(new TrayIconManager.UpdateTrayIconInformation { HideApplication = true });
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
