using System;
using System.Diagnostics;
using HRtoVRChat.Configs;
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
    private UDPListener? _listener;

    public Action<OscMessage?> OnOscMessage { get; set; } = _ => { };

    public OSCService(IOptionsMonitor<AppOptions> appOptions)
    {
        _appOptions = appOptions;
    }

    public void Init()
    {
        if (_listener != null)
        {
            try { _listener.Close(); } catch { }
        }
        _listener = new UDPListener(_appOptions.CurrentValue.ReceiverPort,
            packet => OnOscMessage.Invoke((OscMessage?)packet));
    }

    public bool Detect()
    {
        var processes = Process.GetProcessesByName("VRChat").Length;
        if (_appOptions.CurrentValue.NeosBridge)
            processes += Process.GetProcessesByName("Neos").Length;
        if (_appOptions.CurrentValue.ExpandCVR)
            processes += Process.GetProcessesByName("ChilloutVR").Length;
        return processes > 0;
    }

    public void SendMessage(string destination, object data)
    {
        var realdata = data;
        // If it's a bool, it needs to be converted to a 0, 1 format
        if (Type.GetTypeCode(realdata.GetType()) == TypeCode.Boolean && _appOptions.CurrentValue.UseLegacyBool)
        {
            var dat = (bool)Convert.ChangeType(realdata, TypeCode.Boolean);
            if (dat)
                realdata = 1;
            else
                realdata = 0;
        }

        var message = new OscMessage(destination, realdata);
        var sender = new UDPSender(_appOptions.CurrentValue.Ip, _appOptions.CurrentValue.Port);
        sender.Send(message);
    }
}
