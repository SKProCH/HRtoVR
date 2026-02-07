using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class StromnoOptions
{
    [Description("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")]
    public string Widget { get; set; } = string.Empty;
}
