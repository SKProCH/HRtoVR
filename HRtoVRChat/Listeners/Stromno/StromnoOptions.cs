using System.ComponentModel;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoOptions
{
    [Description("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")]
    public string Widget { get; set; } = string.Empty;
}
