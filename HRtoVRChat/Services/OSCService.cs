using System;
using System.Diagnostics;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using Microsoft.Extensions.Options;
using Vizcon.OSC;

namespace HRtoVRChat.Services;

public interface IOSCService
{
    Action<OscMessage?> OnOscMessage { get; set; }
    void Init();
    bool Detect();
    void SendMessage(string destination, object data);
}

public class OSCService : IOSCService
{
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IOptionsMonitor<VRChatOSCOptions> _vrcOptions;
    private readonly IOptionsMonitor<NeosOptions> _neosOptions;
    private UDPListener? _listener;

    public Action<OscMessage?> OnOscMessage { get; set; } = _ => { };

    public OSCService(IOptionsMonitor<AppOptions> appOptions, IOptionsMonitor<VRChatOSCOptions> vrcOptions, IOptionsMonitor<NeosOptions> neosOptions)
    {
        _appOptions = appOptions;
        _vrcOptions = vrcOptions;
        _neosOptions = neosOptions;
    }

    public void Init()
    {
        if (_listener != null)
        {
            try { _listener.Close(); } catch { }
        }
        _listener = new UDPListener(_vrcOptions.CurrentValue.ReceiverPort,
            packet => OnOscMessage.Invoke((OscMessage?)packet));
    }

    public bool Detect()
    {
        var processes = 0;
        if (_appOptions.CurrentValue.GameHandlers.TryGetValue("VRChatOSC", out var vrcEnabled) && vrcEnabled)
        {
            processes += Process.GetProcessesByName("VRChat").Length;
            if (_vrcOptions.CurrentValue.ExpandCVR)
                processes += Process.GetProcessesByName("ChilloutVR").Length;
        }
        if (_appOptions.CurrentValue.GameHandlers.TryGetValue("Neos", out var neosEnabled) && neosEnabled)
            processes += Process.GetProcessesByName("Neos").Length;

        return processes > 0;
    }

    public void SendMessage(string destination, object data)
    {
        var realdata = data;
        // If it's a bool, it needs to be converted to a 0, 1 format
        if (Type.GetTypeCode(realdata.GetType()) == TypeCode.Boolean && _vrcOptions.CurrentValue.UseLegacyBool)
        {
            var dat = (bool)Convert.ChangeType(realdata, TypeCode.Boolean);
            if (dat)
                realdata = 1;
            else
                realdata = 0;
        }

        var message = new OscMessage(destination, realdata);
        var sender = new UDPSender(_vrcOptions.CurrentValue.Ip, _vrcOptions.CurrentValue.Port);
        sender.Send(message);
    }
}
