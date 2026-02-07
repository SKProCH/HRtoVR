using System;
using System.Reactive;
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

    public UpdatesViewModel()
    {
        RefreshUpdatesCommand = ReactiveCommand.Create(RefreshUpdates);
        UpdateSoftwareCommand = ReactiveCommand.Create(UpdateSoftware);

        Initialize();
    }

    private void Initialize()
    {
        RefreshUpdates();
        if (!SoftwareManager.IsInstalled)
            UpdateButtonText = "INSTALL SOFTWARE";

        SoftwareManager.RequestUpdateProgressBars += (x, y) =>
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                TotalProgress = x;
                TaskProgress = y;
            });
        };
    }

    private void RefreshUpdates()
    {
        LatestVersion = "Latest Version: " + SoftwareManager.GetLatestVersion();
        InstalledVersion = "Installed Version: " + SoftwareManager.GetInstalledVersion();
    }

    private async void UpdateSoftware()
    {
        await SoftwareManager.InstallSoftware(() => {
            Dispatcher.UIThread.InvokeAsync(() => {
                UpdateButtonText = "UPDATE SOFTWARE";
            });
        });
    }
}
