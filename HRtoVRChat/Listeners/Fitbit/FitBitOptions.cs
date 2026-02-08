using System.ComponentModel;
using PropertyModels.ComponentModel;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Fitbit;

public class FitBitOptions : ReactiveObject
{
    [Description("(FitbitHRtoWS Only) The WebSocket to listen to data")]
    public string Url { get; set; } = "ws://localhost:8080/";
}
