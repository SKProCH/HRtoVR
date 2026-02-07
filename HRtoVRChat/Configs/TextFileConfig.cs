using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class TextFileConfig
{
    [TommyComment("(TextFile Only) The location of the text file to pull HeartRate Data from")] [TommyInclude]
    public string textfilelocation = string.Empty;
}
