using System.ComponentModel;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Listeners.PulsoidSocket;

public class PulsoidSocketOptions : ReactiveObject
{
    [Description("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")]
    public string Key { get; set; } = string.Empty;
}
