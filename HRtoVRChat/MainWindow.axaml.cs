using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HRtoVRChat.ViewModels;
using HRtoVRChat_OSC_SDK;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace HRtoVRChat;

public partial class MainWindow : Window {

    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        // View delegates handled by services now

        // Set Instances (Legacy/Static fallback, ideally handled by DI setup)
        TrayIconManager.MainWindow = this;
        TrayIconManager.ArgumentsWindow = new Arguments();
    }
}
