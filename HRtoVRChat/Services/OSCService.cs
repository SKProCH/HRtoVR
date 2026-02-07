using System;
using System.Diagnostics;
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
    private readonly IConfigService _configService;
    private UDPListener? _listener;

    public Action<OscMessage?> OnOscMessage { get; set; } = _ => { };

    public OSCService(IConfigService configService)
    {
        _configService = configService;
    }

    public void Init()
    {
        if (_listener != null)
        {
            try { _listener.Close(); } catch { }
        }
        _listener = new UDPListener(_configService.LoadedConfig.receiverPort,
            packet => OnOscMessage.Invoke((OscMessage?)packet));
    }

    public bool Detect()
    {
        var processes = Process.GetProcessesByName("VRChat").Length;
        if (HRService.Gargs.Contains("--neos-bridge"))
            processes += Process.GetProcessesByName("Neos").Length;
        if (_configService.LoadedConfig.ExpandCVR)
            processes += Process.GetProcessesByName("ChilloutVR").Length;
        return processes > 0;
    }

    public void SendMessage(string destination, object data)
    {
        var realdata = data;
        // If it's a bool, it needs to be converted to a 0, 1 format
        if (Type.GetTypeCode(realdata.GetType()) == TypeCode.Boolean && HRService.Gargs.Contains("--use-01-bool"))
        {
            var dat = (bool)Convert.ChangeType(realdata, TypeCode.Boolean);
            if (dat)
                realdata = 1;
            else
                realdata = 0;
        }

        var message = new OscMessage(destination, realdata);
        var sender = new UDPSender(_configService.LoadedConfig.ip, _configService.LoadedConfig.port);
        sender.Send(message);
    }
}
