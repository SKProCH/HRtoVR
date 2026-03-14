using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HRtoVRChat.Models;

namespace HRtoVRChat.Controls;

public partial class ConnectionIndicator : UserControl
{
    public static readonly StyledProperty<ConnectionState> StateProperty =
        AvaloniaProperty.Register<ConnectionIndicator, ConnectionState>(nameof(State), ConnectionState.Disconnected);

    public ConnectionState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public ConnectionIndicator()
    {
        InitializeComponent();
    }
}
