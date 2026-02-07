using System.Diagnostics;
using HRtoVRChat.Services;

namespace HRtoVRChat.GameHandlers;

public class VRChatOSCHandler : IGameHandler {
    private readonly IParamsService _paramsService;
    private readonly IConfigService _configService;

    public VRChatOSCHandler(IParamsService paramsService, IConfigService configService)
    {
        _paramsService = paramsService;
        _configService = configService;
    }

    public string Name => "VRChat";

    public bool IsGameRunning() {
        bool vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
        bool cvrRunning = _configService.LoadedConfig.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
        return vrcRunning || cvrRunning;
    }

    public void Init() {
        // Ensure params are ready?
        // ParamsManager.InitParams() is usually called when VRChat is detected.
    }

    public void Start() {
        LogHelper.Log("Starting VRChat OSC Handler");
        _paramsService.InitParams();
    }

    public void Stop() {
        LogHelper.Log("Stopping VRChat OSC Handler");
        _paramsService.ResetParams();
    }

    public void UpdateHR(int ones, int tens, int hundreds, int hr, bool isConnected, bool isActive) {
        var hro = new HROutput {
            ones = ones,
            tens = tens,
            hundreds = hundreds,
            HR = hr,
            isConnected = isConnected,
            isActive = isActive
        };
        _paramsService.UpdateHRValues(hro);
    }

    public void UpdateHeartBeat(bool isHeartBeat, bool shouldStart) {
        _paramsService.UpdateHeartBeat(isHeartBeat);
    }
}
