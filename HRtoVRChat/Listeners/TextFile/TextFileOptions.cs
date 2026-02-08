using System.ComponentModel;
using PropertyModels.ComponentModel;

namespace HRtoVRChat.Listeners.TextFile;

public class TextFileOptions : ReactiveObject
{
    [Description("(TextFile Only) The location of the text file to pull HeartRate Data from")]
    public string Location { get; set; } = string.Empty;
}
