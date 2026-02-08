using Avalonia;
using Avalonia.Controls;

namespace HRtoVRChat;

public partial class MainWindow : Window {

    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}
