using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.PulsoidSocket;

public class PulsoidSocketOptions : ReactiveObject
{
    [Reactive]
    [Description("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")]
    public string Key { get; set; } = string.Empty;
}
