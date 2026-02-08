using System.Reactive;
using ReactiveUI;

namespace HRtoVRChat.ViewModels;

public class ListenersViewModel : ViewModelBase
{
    public ConfigViewModel ConfigVM { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

    public ListenersViewModel(ConfigViewModel configVM)
    {
        ConfigVM = configVM;
        OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);
    }
}
