using Avalonia;
using Avalonia.Controls;
using HRtoVRChat.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HRtoVRChat;

public partial class ParameterNames : Window {
    public static bool IsOpen;

    public ParameterNames() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        if (Application.Current is App app && app.Services != null)
        {
            DataContext = app.Services.GetRequiredService<ParameterNamesViewModel>();
        }
        Closed += (sender, args) => IsOpen = false;
    }


    public override void Show() {
        base.Show();
        IsOpen = true;
    }
}
