using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class PulsoidSocketOptions
{
    [Description("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")]
    public string Key { get; set; } = string.Empty;
}
