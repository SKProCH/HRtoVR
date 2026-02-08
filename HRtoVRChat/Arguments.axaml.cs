using Avalonia;
using Avalonia.Controls;
using HRtoVRChat.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HRtoVRChat;

public partial class Arguments : Window {
    public Arguments() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        if (Application.Current is App app && app.Services != null)
        {
            var vm = app.Services.GetRequiredService<ArgumentsViewModel>();
            DataContext = vm;

            Closed += (sender, args) => {
                vm.SaveConfig();
                // Re-creating the window on close is tricky with DI.
                // We should probably just Hide() it instead of closing, or let the TrayIconService handle creating a NEW one.
                // For now, let's just null it out in TrayIconManager (which is static) but we are using TrayIconService (instance).
                // This is a bit of a legacy/DI conflict.
                // TrayIconService updates TrayIconManager.ArgumentsWindow.
                // Ideally, TrayIconService should create the window when needed.
                // For now, we won't re-assign it here.
            };
        }
    }
}