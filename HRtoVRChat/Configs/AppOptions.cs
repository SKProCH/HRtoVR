using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Configs;

public class AppOptions : ReactiveObject {
    // Main Config properties
    [Description("The source from where to pull Heart Rate Data")]
    public string ActiveListener { get; set; } = "unknown";

    [Description("Enabled Game Handlers")] 
    public Dictionary<string, bool> GameHandlers { get; set; } = [];

    [Description("The maximum HR for HRPercent")]
    public double MaxHR { get; set; } = 255;

    [Description("The minimum HR for HRPercent")]
    public double MinHR { get; set; } = 0;

    [Description("A dictionary containing what names to use for default parameters.")]
    public ParameterNamesOptions ParameterNames { get; set; } = new();
}