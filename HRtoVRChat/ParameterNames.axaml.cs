using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HRtoVRChat.ViewModels;

namespace HRtoVRChat;

public partial class ParameterNames : Window {
    public static bool IsOpen;

    public ParameterNames() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        DataContext = new ParameterNamesViewModel();
        Closed += (sender, args) => IsOpen = false;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Show() {
        base.Show();
        IsOpen = true;
    }
}
