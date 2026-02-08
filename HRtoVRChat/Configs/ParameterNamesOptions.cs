using PropertyModels.ComponentModel;

namespace HRtoVRChat.Configs;

public class ParameterNamesOptions : ReactiveObject
{
    public string OnesHR { get; set; } = "onesHR";
    public string TensHR { get; set; } = "tensHR";
    public string HundredsHR { get; set; } = "hundredsHR";
    public string IsHRConnected { get; set; } = "isHRConnected";
    public string IsHRActive { get; set; } = "isHRActive";
    public string IsHRBeat { get; set; } = "isHRBeat";
    public string HRPercent { get; set; } = "HRPercent";
    public string FullHRPercent { get; set; } = "FullHRPercent";
    public string HR { get; set; } = "HR";
}
