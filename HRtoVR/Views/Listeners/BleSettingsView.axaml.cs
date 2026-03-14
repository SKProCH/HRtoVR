using Avalonia.ReactiveUI;
using HRtoVR.ViewModels.Listeners;

namespace HRtoVR.Views.Listeners;

public partial class BleSettingsView : ReactiveUserControl<BleSettingsViewModel> {
    public BleSettingsView() {
        InitializeComponent();
    }
}