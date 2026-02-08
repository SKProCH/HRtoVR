namespace HRtoVRChat.ViewModels.Listeners;

public class ConfigSettingsViewModel : ViewModelBase, IListenerSettingsViewModel
{
    public object Settings { get; }

    public ConfigSettingsViewModel(object settings)
    {
        Settings = settings;
    }
}
