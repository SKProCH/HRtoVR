using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.Listeners.Fitbit;

public class FitBitOptions : ReactiveObject {
    [Reactive]
    [Description("(FitbitHRtoWS Only) The WebSocket to listen to data")]
    public string Url { get; set; } = "ws://localhost:8080/";
}