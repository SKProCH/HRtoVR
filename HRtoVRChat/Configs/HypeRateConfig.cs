using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class HypeRateConfig
{
    [TommyComment("(HypeRate Only) The code to pull HypeRate Data from")] [TommyInclude]
    public string hyperateSessionId = string.Empty;
}
