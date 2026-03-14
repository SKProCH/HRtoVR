using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia;
using HRtoVRChat.Services;
using HRtoVRChat.ViewModels.GameHandlers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public IPageViewModel CurrentPage { get; set; }

    public ObservableCollection<IPageViewModel> Pages { get; }

    // Commands
    public ReactiveCommand<IPageViewModel, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    // Events
    public event Action? RequestHide;

    private readonly ITrayIconService _trayIconService;

    public MainWindowViewModel(
        IEnumerable<IPageViewModel> pages,
        ITrayIconService trayIconService)
    {
        Pages = new ObservableCollection<IPageViewModel>(pages);
        _trayIconService = trayIconService;

        // Global Initialization
        if (!string.IsNullOrEmpty(App.LocalDirectory) && !Directory.Exists(App.LocalDirectory))
            Directory.CreateDirectory(App.LocalDirectory);

        // _configService.CreateConfig(); // Config is loaded via DI

        // Default Page
        CurrentPage = Pages.FirstOrDefault() ?? throw new InvalidOperationException("No pages registered");

        // Commands
        SwitchPanelCommand = ReactiveCommand.Create<IPageViewModel>(vm =>
        {
            CurrentPage = vm;
        });

        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);

        HideAppCommand = ReactiveCommand.Create(() =>
        {
             _trayIconService.Update(new TrayIconInfo { HideApplication = true });
             RequestHide?.Invoke();
        });

        ExitAppCommand = ReactiveCommand.CreateFromTask(async () => {
            await App.Shutdown();
        });
    }
}
