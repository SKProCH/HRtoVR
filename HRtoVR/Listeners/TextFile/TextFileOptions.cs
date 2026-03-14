using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.TextFile;

public class TextFileOptions : ReactiveObject
{
    [Reactive]
    [Description("(TextFile Only) The location of the text file to pull HeartRate Data from")]
    public string Location { get; set; } = string.Empty;
}
