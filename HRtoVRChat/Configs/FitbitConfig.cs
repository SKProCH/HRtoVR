using Tommy.Serializer;

namespace HRtoVRChat.Configs;

public class FitbitConfig
{
    [TommyComment("(FitbitHRtoWS Only) The WebSocket to listen to data")] [TommyInclude]
    public string fitbitURL = "ws://localhost:8080/";
}
