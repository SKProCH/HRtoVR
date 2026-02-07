using System;
using System.Reactive;
using HRtoVRChat.Services;
using HRtoVRChat.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Avalonia.Threading;

namespace HRtoVRChat.ViewModels;

public class UpdatesViewModel : ViewModelBase
{
    [Reactive] public string InstalledVersion { get; set; } = "";
    [Reactive] public string LatestVersion { get; set; } = "";
    [Reactive] public string UpdateButtonText { get; set; } = "UPDATE SOFTWARE";
    [Reactive] public double TotalProgress { get; set; }
    [Reactive] public double TaskProgress { get; set; }

    public ReactiveCommand<Unit, Unit> RefreshUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateSoftwareCommand { get; }

    private readonly ISoftwareService _softwareService;

    public UpdatesViewModel(ISoftwareService softwareService)
    {
        _softwareService = softwareService;

        RefreshUpdatesCommand = ReactiveCommand.Create(RefreshUpdates);
        UpdateSoftwareCommand = ReactiveCommand.Create(UpdateSoftware);

        Initialize();
    }

    private void Initialize()
    {
        RefreshUpdates();
        if (!_softwareService.IsInstalled)
            UpdateButtonText = "INSTALL SOFTWARE";

        _softwareService.RequestUpdateProgressBars += (x, y) =>
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                TotalProgress = x;
                TaskProgress = y;
            });
        };
    }

    private void RefreshUpdates()
    {
        LatestVersion = "Latest Version: " + _softwareService.GetLatestVersion();
        InstalledVersion = "Installed Version: " + _softwareService.GetInstalledVersion();
    }

    private async void UpdateSoftware()
    {
        await _softwareService.InstallSoftware(() => {
            Dispatcher.UIThread.InvokeAsync(() => {
                UpdateButtonText = "UPDATE SOFTWARE";
            });
        });
    }
}
