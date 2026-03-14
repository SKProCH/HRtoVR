using Avalonia;
using Avalonia.Controls;

namespace HRtoVR;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}