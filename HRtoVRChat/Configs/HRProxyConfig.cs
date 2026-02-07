using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class HRProxyConfig
{
    [TommyComment("(HRProxy Only) The code to pull HRProxy Data from")] [TommyInclude]
    public string hrproxyId = string.Empty;
}
