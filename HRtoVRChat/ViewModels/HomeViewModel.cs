using System.Reactive;
using HRtoVRChat.Services;
using ReactiveUI;

namespace HRtoVRChat.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public HomeViewModel(IBrowserService browserService)
    {
        OpenUrlCommand = ReactiveCommand.Create<string>(browserService.OpenUrl);
    }
}
