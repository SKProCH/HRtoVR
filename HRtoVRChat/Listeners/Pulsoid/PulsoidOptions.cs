using System.ComponentModel;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Listeners.Pulsoid;

public class PulsoidOptions : ReactiveObject
{
    [Description("(Pulsoid Only) The widgetId to pull HeartRate Data from")]
    public string Widget { get; set; } = string.Empty;
}
