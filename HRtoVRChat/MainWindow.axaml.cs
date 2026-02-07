using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HRtoVRChat.ViewModels;
using HRtoVRChat_OSC_SDK;

namespace HRtoVRChat;

public partial class MainWindow : Window {

    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}
