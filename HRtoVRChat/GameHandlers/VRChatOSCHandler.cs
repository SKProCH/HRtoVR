using System.Diagnostics;
using HRtoVRChat.Configs;
using HRtoVRChat.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.GameHandlers;

public class VRChatOSCHandler : IGameHandler {
    private readonly IParamsService _paramsService;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger _logger;

    public VRChatOSCHandler(IParamsService paramsService, IOptionsMonitor<AppOptions> appOptions, ILogger<VRChatOSCHandler> logger)
    {
        _paramsService = paramsService;
        _appOptions = appOptions;
        _logger = logger;
    }

    public string Name => "VRChat";

    public bool IsGameRunning() {
        bool vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
        bool cvrRunning = _appOptions.CurrentValue.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
        return vrcRunning || cvrRunning;
    }

    public void Init() {
        // Ensure params are ready?
        // ParamsManager.InitParams() is usually called when VRChat is detected.
    }

    public void Start() {
        _logger.LogInformation("Starting VRChat OSC Handler");
        _paramsService.InitParams();
    }

    public void Stop() {
        _logger.LogInformation("Stopping VRChat OSC Handler");
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
