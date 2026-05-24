using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    [Reactive] public IPageViewModel CurrentPage { get; set; }

    public ObservableCollection<IPageViewModel> Pages { get; }

    // Commands
    public ReactiveCommand<IPageViewModel, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public event Action? RequestHide;

    public MainWindowViewModel(IEnumerable<IPageViewModel> pages) {
        Pages = new ObservableCollection<IPageViewModel>(pages);

        if (!string.IsNullOrEmpty(App.LocalDirectory) && !Directory.Exists(App.LocalDirectory))
            Directory.CreateDirectory(App.LocalDirectory);

        CurrentPage = Pages.FirstOrDefault() ?? throw new InvalidOperationException("No pages registered");

        SwitchPanelCommand = ReactiveCommand.Create<IPageViewModel>(vm => {
            CurrentPage = vm;
        });

        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);

        HideAppCommand = ReactiveCommand.Create(() => {
            RequestHide?.Invoke();
        });

        ExitAppCommand = ReactiveCommand.CreateFromTask(async () => {
            await App.Shutdown();
        });
    }
}
