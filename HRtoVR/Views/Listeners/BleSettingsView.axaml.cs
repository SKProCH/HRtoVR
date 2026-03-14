using Avalonia.ReactiveUI;
using HRtoVRChat.ViewModels.Listeners;
using ReactiveUI;

namespace HRtoVRChat.Views.Listeners;

public partial class BleSettingsView : ReactiveUserControl<BleSettingsViewModel>
{
    public BleSettingsView()
    {
        InitializeComponent();
    }
}
