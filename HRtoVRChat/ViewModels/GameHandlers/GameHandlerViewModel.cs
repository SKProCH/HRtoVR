using HRtoVRChat.ViewModels.Listeners;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels.GameHandlers;

public class GameHandlerViewModel : ViewModelBase
{
    public string Name { get; set; } = "";
    [Reactive] public bool IsRunning { get; set; }
    [Reactive] public bool IsEnabled { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public IListenerSettingsViewModel? Settings { get; set; }

    public GameHandlerViewModel(string name)
    {
        Name = name;
    }
}
