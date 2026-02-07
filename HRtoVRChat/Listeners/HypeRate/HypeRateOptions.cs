using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class HypeRateOptions
{
    [Description("(HypeRate Only) The code to pull HypeRate Data from")]
    public string SessionId { get; set; } = string.Empty;
}
