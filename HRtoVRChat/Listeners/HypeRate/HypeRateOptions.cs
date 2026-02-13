using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.HypeRate;

public class HypeRateOptions : ReactiveObject
{
    [Reactive]
    [Description("(HypeRate Only) The code to pull HypeRate Data from")]
    public string SessionId { get; set; } = string.Empty;
}
