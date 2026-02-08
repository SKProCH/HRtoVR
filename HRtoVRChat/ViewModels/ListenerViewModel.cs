using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ListenerViewModel : ViewModelBase
{
    [Reactive] public bool IsExpanded { get; set; }
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    public string Name { get; set; } = "";
    public ObservableCollection<ConfigItemViewModel> Settings { get; } = new();

    public ListenerViewModel(string name)
    {
        Name = name;
    }
}
