using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class StromnoConfig
{
    [TommyComment("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")] [TommyInclude]
    public string stromnowidget = string.Empty;
}
