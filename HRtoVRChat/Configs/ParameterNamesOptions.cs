using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Configs;

public class ParameterNamesOptions : ReactiveObject
{
    [Reactive] public string OnesHR { get; set; } = "onesHR";
    [Reactive] public string TensHR { get; set; } = "tensHR";
    [Reactive] public string HundredsHR { get; set; } = "hundredsHR";
    [Reactive] public string IsHRConnected { get; set; } = "isHRConnected";
    [Reactive] public string IsHRActive { get; set; } = "isHRActive";
    [Reactive] public string IsHRBeat { get; set; } = "isHRBeat";
    [Reactive] public string HRPercent { get; set; } = "HRPercent";
    [Reactive] public string FullHRPercent { get; set; } = "FullHRPercent";
    [Reactive] public string HR { get; set; } = "HR";
}
