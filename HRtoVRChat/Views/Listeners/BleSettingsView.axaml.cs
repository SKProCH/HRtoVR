using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HRtoVRChat.Views.Listeners;

public partial class BleSettingsView : UserControl
{
    public BleSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
