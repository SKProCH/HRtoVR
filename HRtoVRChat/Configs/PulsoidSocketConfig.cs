using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class PulsoidSocketConfig
{
    [TommyComment("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")] [TommyInclude]
    public string pulsoidkey = string.Empty;
}
