using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HRtoVRChat.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;

namespace HRtoVRChat.ViewModels;

public class ProgramViewModel : ViewModelBase
{
    [Reactive] public string StatusText { get; set; } = "STOPPED";
    [Reactive] public string CommandInput { get; set; } = "";

    public ReactiveCommand<Unit, Unit> StartCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<Unit, Unit> KillCommand { get; }
    public ReactiveCommand<Unit, Unit> SendCommandCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenArgumentsCommand { get; }

    public event Action<string?, string>? OnLogReceived;

    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ISoftwareService _softwareService;
    private readonly ITrayIconService _trayIconService;

    public ProgramViewModel(ISoftwareService softwareService, ITrayIconService trayIconService)
    {
        _softwareService = softwareService;
        _trayIconService = trayIconService;

        StartCommand = ReactiveCommand.Create(StartSoftware);
        StopCommand = ReactiveCommand.Create(StopSoftware);
        KillCommand = ReactiveCommand.Create(KillSoftware);
        SendCommandCommand = ReactiveCommand.Create(SendCommand);
        OpenArgumentsCommand = ReactiveCommand.Create(() => _trayIconService.ArgumentsWindow?.Show());

        Initialize();
    }

    private void Initialize()
    {
        _softwareService.OnConsoleUpdate += (message, overrideColor) =>
        {
             OnLogReceived?.Invoke(message, overrideColor ?? "");
        };

        // Initial status update
        UpdateStatus();
        StartBackgroundThread();
    }

    public void UpdateStatus()
    {
        StatusText = "STATUS: " + (_softwareService.IsSoftwareRunning ? "RUNNING" : "STOPPED");
    }

    private void StartSoftware()
    {
        OnLogReceived?.Invoke(null, "CLEAR");
        _softwareService.OnConsoleUpdate(
            $"HRtoVRChat {_softwareService.GetInstalledVersion()} Created by 200Tigersbloxed\n", string.Empty);
        _softwareService.StartSoftware();
        UpdateStatus();
    }

    private void StopSoftware()
    {
        _softwareService.StopSoftware();
        UpdateStatus();
    }

    private void KillSoftware()
    {
        _softwareService.StopSoftware();
        try {
            foreach (var process in Process.GetProcessesByName("HRtoVRChat")) {
                process.Kill();
            }
        }
        catch (Exception) { }
        UpdateStatus();
    }

    private void SendCommand()
    {
        if (!string.IsNullOrEmpty(CommandInput))
        {
            _softwareService.SendCommand(CommandInput);
            CommandInput = "";
        }
    }

    private void StartBackgroundThread()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => {
            while (!_cancellationTokenSource.IsCancellationRequested) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    UpdateStatus();

                    _trayIconService.Update(new TrayIconManager.UpdateTrayIconInformation {
                        Status = _softwareService.IsSoftwareRunning ? "RUNNING" : "STOPPED"
                    });
                });

                Thread.Sleep(100); // Poll every 100ms
            }
        }, _cancellationTokenSource.Token);
    }
}
