using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class HRProxyOptions
{
    [Description("(HRProxy Only) The code to pull HRProxy Data from")]
    public string Id { get; set; } = string.Empty;
}
