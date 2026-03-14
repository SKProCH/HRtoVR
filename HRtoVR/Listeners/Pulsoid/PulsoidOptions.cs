using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Pulsoid;

public class PulsoidOptions : ReactiveObject
{
    [Reactive]
    [Description("(Pulsoid Only) The widgetId to pull HeartRate Data from")]
    public string Widget { get; set; } = string.Empty;
}
