using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
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

    public MainWindowViewModel()
    {
        // Global Initialization
        if (!string.IsNullOrEmpty(SoftwareManager.LocalDirectory) && !Directory.Exists(SoftwareManager.LocalDirectory))
            Directory.CreateDirectory(SoftwareManager.LocalDirectory);

        ConfigManager.CreateConfig();

        // Initialize Sub-ViewModels
        HomeVM = new HomeViewModel(OpenBrowser);
        ProgramVM = new ProgramViewModel();
        UpdatesVM = new UpdatesViewModel();
        ConfigVM = new ConfigViewModel();
        IncomingDataVM = new IncomingDataViewModel();

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

        OpenUrlCommand = ReactiveCommand.Create<string>(OpenBrowser);

        HideAppCommand = ReactiveCommand.Create(() =>
        {
             TrayIconManager.Update(new TrayIconManager.UpdateTrayIconInformation { HideApplication = true });
             RequestHide?.Invoke();
        });

        ExitAppCommand = ReactiveCommand.Create(() =>
        {
            // Stop software if running
            SoftwareManager.StopSoftware();
            try {
                foreach (var process in Process.GetProcessesByName("HRtoVRChat_OSC")) {
                    process.Kill();
                }
            }
            catch (Exception) { }
            Environment.Exit(0);
        });
    }

    private void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start("open", url);
        }
        else {
            try {
                // Fallback
                if (url.Contains("github"))
                    Process.Start("https://github.com/200Tigersbloxed/HRtoVRChat_OSC");
            }
            catch (Exception) { }
        }
    }
}
