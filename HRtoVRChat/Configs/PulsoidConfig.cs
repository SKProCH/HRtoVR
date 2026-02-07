using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class PulsoidConfig
{
    [TommyComment("(Pulsoid Only) The widgetId to pull HeartRate Data from")] [TommyInclude]
    public string pulsoidwidget = string.Empty;
}
