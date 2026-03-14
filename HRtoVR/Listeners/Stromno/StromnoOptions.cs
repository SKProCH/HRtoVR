using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoOptions : ReactiveObject
{
    [Reactive]
    [Description("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")]
    public string Widget { get; set; } = string.Empty;
}
