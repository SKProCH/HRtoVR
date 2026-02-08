using System.Collections.ObjectModel;

namespace HRtoVRChat.ViewModels;

public class ListenerViewModel : ViewModelBase
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = ""; // The string for hrType
    public ObservableCollection<ConfigItemViewModel> Settings { get; } = new();

    public ListenerViewModel(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
