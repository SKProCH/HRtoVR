using System.Collections.ObjectModel;
using ReactiveUI;

namespace HRtoVRChat.ViewModels;

public class ManagerViewModel : ViewModelBase
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = ""; // The string for hrType
    public ObservableCollection<ConfigItemViewModel> Settings { get; } = new();

    public ManagerViewModel(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
