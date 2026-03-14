using System.Collections.Generic;
using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Configs;

public class AppOptions : EditableAppOptions {
    [Reactive]
    public string? ActiveListener { get; set; }

    [Reactive]
    public Dictionary<string, bool> GameHandlers { get; set; } = [];
    
    [Reactive]
    public ParameterNamesOptions ParameterNames { get; set; } = new();
}

public class EditableAppOptions : ReactiveObject {
    [Reactive]
    [Description("The maximum HR for HRPercent")]
    public int MaxHR { get; set; } = 255;

    [Reactive]
    [Description("The minimum HR for HRPercent")]
    public int MinHR { get; set; } = 0;
}