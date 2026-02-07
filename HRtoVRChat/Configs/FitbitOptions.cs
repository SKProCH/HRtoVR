using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class FitbitOptions
{
    [Description("(FitbitHRtoWS Only) The WebSocket to listen to data")]
    public string Url { get; set; } = "ws://localhost:8080/";
}
