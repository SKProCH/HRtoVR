using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HRtoVRChat.ViewModels;

namespace HRtoVRChat;

public partial class Arguments : Window {
    public Arguments() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        var vm = new ArgumentsViewModel();
        DataContext = vm;

        Closed += (sender, args) => {
            vm.SaveConfig();
            TrayIconManager.ArgumentsWindow = new Arguments();
        };
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}