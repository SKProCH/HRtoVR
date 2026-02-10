using System.Diagnostics;
using System.Threading.Tasks;
using HRtoVRChat.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Vizcon.OSC;

namespace HRtoVRChat.GameHandlers;

public class VRChatOSCHandler : ReactiveObject, IGameHandler {
    private readonly IParamsService _paramsService;
    private readonly IOptionsMonitor<VRChatOSCOptions> _options;
    private readonly ILogger _logger;

    private UDPListener? _listener;
    private UDPSender? _sender;

    public string Name => "VRChatOSC";
    [Reactive] public bool IsConnected { get; private set; }

    public VRChatOSCHandler(IParamsService paramsService, IOptionsMonitor<VRChatOSCOptions> options, ILogger<VRChatOSCHandler> logger)
    {
        _paramsService = paramsService;
        _options = options;
        _logger = logger;
    }

    private bool _active;

    public void Start() {
        _logger.LogInformation("Starting VRChat OSC Handler");
        _sender = new UDPSender(_options.CurrentValue.Ip, _options.CurrentValue.Port);
        _listener = new UDPListener(_options.CurrentValue.ReceiverPort, packet => OnOscMessage((OscMessage?)packet));
        _paramsService.InitParams(SendMessage);
        _active = true;
        // Start a task to periodically update IsConnected
        _ = Task.Run(async () => {
            while (_active) {
                var vrcRunning = Process.GetProcessesByName("VRChat").Length > 0;
                var cvrRunning = _options.CurrentValue.ExpandCVR && Process.GetProcessesByName("ChilloutVR").Length > 0;
                IsConnected = vrcRunning || cvrRunning;
                await Task.Delay(2000);
            }
        });
    }

    public void Stop() {
        _logger.LogInformation("Stopping VRChat OSC Handler");
        _active = false;
        try
        {
            _listener?.Close();
            _sender = null;
        }
        catch { }
        _paramsService.ResetParams();
        IsConnected = false;
    }

    private void SendMessage(string address, object value)
    {
        var realData = value;
        // If it's a bool, it needs to be converted to a 0, 1 format
        if (value is bool b && _options.CurrentValue.UseLegacyBool)
        {
            realData = b ? 1 : 0;
        }

        var message = new OscMessage(address, realData);
        _sender?.Send(message);
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
