using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HRtoVRChat.ViewModels;
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

    public ProgramViewModel()
    {
        StartCommand = ReactiveCommand.Create(StartSoftware);
        StopCommand = ReactiveCommand.Create(StopSoftware);
        KillCommand = ReactiveCommand.Create(KillSoftware);
        SendCommandCommand = ReactiveCommand.Create(SendCommand);
        OpenArgumentsCommand = ReactiveCommand.Create(() => TrayIconManager.ArgumentsWindow?.Show());

        Initialize();
    }

    private void Initialize()
    {
        SoftwareManager.OnConsoleUpdate += (message, overrideColor) =>
        {
             OnLogReceived?.Invoke(message, overrideColor ?? "");
        };

        // Initial status update
        UpdateStatus();
        StartBackgroundThread();
    }

    public void UpdateStatus()
    {
        StatusText = "STATUS: " + (SoftwareManager.IsSoftwareRunning ? "RUNNING" : "STOPPED");
    }

    private void StartSoftware()
    {
        OnLogReceived?.Invoke(null, "CLEAR");
        SoftwareManager.OnConsoleUpdate(
            $"HRtoVRChat {SoftwareManager.GetInstalledVersion()} Created by 200Tigersbloxed\n", string.Empty);
        SoftwareManager.StartSoftware();
        UpdateStatus();
    }

    private void StopSoftware()
    {
        SoftwareManager.StopSoftware();
        UpdateStatus();
    }

    private void KillSoftware()
    {
        SoftwareManager.StopSoftware();
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
            SoftwareManager.SendCommand(CommandInput);
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

                    TrayIconManager.Update(new TrayIconManager.UpdateTrayIconInformation {
                        Status = SoftwareManager.IsSoftwareRunning ? "RUNNING" : "STOPPED"
                    });
                });

                Thread.Sleep(100); // Poll every 100ms
            }
        }, _cancellationTokenSource.Token);
    }
}
