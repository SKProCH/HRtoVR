using System.Diagnostics;

namespace HRtoVRChat.GameHandlers;

public class VRChatOSCHandler : IGameHandler {
    public string Name => "VRChat";

    public bool IsGameRunning() {
        bool vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
        bool cvrRunning = ConfigManager.LoadedConfig.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
        return vrcRunning || cvrRunning;
    }

    public void Init() {
        // Ensure params are ready?
        // ParamsManager.InitParams() is usually called when VRChat is detected.
    }

    public void Start() {
        LogHelper.Log("Starting VRChat OSC Handler");
        ParamsManager.InitParams();
    }

    public void Stop() {
        LogHelper.Log("Stopping VRChat OSC Handler");
        ParamsManager.ResetParams();
    }

    public void UpdateHR(int ones, int tens, int hundreds, int hr, bool isConnected, bool isActive) {
        var hro = new ParamsManager.HROutput {
            ones = ones,
            tens = tens,
            hundreds = hundreds,
            HR = hr,
            isConnected = isConnected,
            isActive = isActive
        };
        ParamsManager.UpdateHRValues(hro);
    }

    public void UpdateHeartBeat(bool isHeartBeat, bool shouldStart) {
        ParamsManager.UpdateHeartBeat(isHeartBeat);
    }
}
