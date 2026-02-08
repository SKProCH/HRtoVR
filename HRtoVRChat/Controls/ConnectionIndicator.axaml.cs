using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HRtoVRChat.Controls;

public partial class ConnectionIndicator : UserControl
{
    public static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<ConnectionIndicator, bool>(nameof(IsConnected));

    public static readonly StyledProperty<int> HeartRateProperty =
        AvaloniaProperty.Register<ConnectionIndicator, int>(nameof(HeartRate));

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ConnectionIndicator, bool>(nameof(IsActive));

    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    public int HeartRate
    {
        get => GetValue(HeartRateProperty);
        set => SetValue(HeartRateProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    static ConnectionIndicator()
    {
        IsConnectedProperty.Changed.AddClassHandler<ConnectionIndicator>((x, _) => x.UpdateIsActive());
        HeartRateProperty.Changed.AddClassHandler<ConnectionIndicator>((x, _) => x.UpdateIsActive());
    }

    private void UpdateIsActive()
    {
        IsActive = IsConnected && HeartRate > 0;
    }

    public ConnectionIndicator()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
