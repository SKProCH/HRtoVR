using System.Diagnostics;
using HRtoVRChat.Configs;
using HRtoVRChat.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vizcon.OSC;

namespace HRtoVRChat.GameHandlers;

public class VRChatOSCHandler : IGameHandler {
    private readonly IParamsService _paramsService;
    private readonly IOSCService _oscService;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger _logger;

    public VRChatOSCHandler(IParamsService paramsService, IOSCService oscService, IOptionsMonitor<AppOptions> appOptions, ILogger<VRChatOSCHandler> logger)
    {
        _paramsService = paramsService;
        _oscService = oscService;
        _appOptions = appOptions;
        _logger = logger;
    }

    public bool IsRunning() {
        var vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
        var cvrRunning = _appOptions.CurrentValue.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
        return vrcRunning || cvrRunning;
    }

    public void Start() {
        _logger.LogInformation("Starting VRChat OSC Handler");
        _paramsService.InitParams();
        _oscService.OnOscMessage += OnOscMessage;
    }

    public void Stop() {
        _logger.LogInformation("Stopping VRChat OSC Handler");
        _oscService.OnOscMessage -= OnOscMessage;
        _paramsService.ResetParams();
    }

    private void OnOscMessage(OscMessage? message)
    {
        if (message?.Address == "/avatar/change")
        {
            _logger.LogInformation("Avatar change detected! Resending parameters...");
            _paramsService.ForceResendValues();
        }
    }

    public void Update(int heartBeat, bool isConnected)
    {
        // Split heartBeat into ones, tens, hundreds
        var hundreds = heartBeat / 100 % 10;
        var tens = heartBeat / 10 % 10;
        var ones = heartBeat % 10;

        var hro = new HROutput {
            ones = ones,
            tens = tens,
            hundreds = hundreds,
            HR = heartBeat,
            isConnected = isConnected,
            isActive = isConnected
        };
        _paramsService.UpdateHRValues(hro);
    }
}
