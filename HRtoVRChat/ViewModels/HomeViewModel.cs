using System.Reactive;
using ReactiveUI;

namespace HRtoVRChat.ViewModels;

public class HomeViewModel : ViewModelBase
{
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public HomeViewModel()
    {
        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);
    }
}
