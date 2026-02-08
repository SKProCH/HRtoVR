using System.ComponentModel;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoOptions : ReactiveObject
{
    [Description("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")]
    public string Widget { get; set; } = string.Empty;
}
