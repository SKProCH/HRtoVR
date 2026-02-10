using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Configs;

public class AppOptions : EditableAppOptions {
    public string? ActiveListener { get; set; }

    public Dictionary<string, bool> GameHandlers { get; set; } = [];
    
    public ParameterNamesOptions ParameterNames { get; set; } = new();
}

public class EditableAppOptions : ReactiveObject {
    [Description("The maximum HR for HRPercent")]
    public int MaxHR { get; set; } = 255;

    [Description("The minimum HR for HRPercent")]
    public int MinHR { get; set; } = 0;
}