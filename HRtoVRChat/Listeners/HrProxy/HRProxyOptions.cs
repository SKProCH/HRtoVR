using  System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.HrProxy;

public class HRProxyOptions : ReactiveObject
{
    [Reactive]
    [Description("(HRProxy Only) The code to pull HRProxy Data from")]
    public string Id { get; set; } = string.Empty;
}
