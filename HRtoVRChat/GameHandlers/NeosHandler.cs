using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Options;
using WatsonWebsocket;

namespace HRtoVRChat.GameHandlers;

public class NeosHandler : IGameHandler {
    public static Action<string> OnCommand = s => { };
    private WatsonWsServer? _server;
    private NeosMessage _neosMessage = new();
    private readonly IOptionsMonitor<AppOptions> _appOptions;

    public NeosHandler(IOptionsMonitor<AppOptions> appOptions)
    {
        _appOptions = appOptions;
    }

    public string Name => "Neos";

    public bool IsGameRunning() {
        return Process.GetProcessesByName("Neos").Length > 0;
    }

    public void Init() {
        _server = new WatsonWsServer("127.0.0.1", 4206, false);
        _server.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        var msg = Encoding.UTF8.GetString(e.Data);
        OnCommand.Invoke(msg);
    }

    public void Start() {
        if (_server == null) Init();

        try
        {
            _server?.Start();
        }
        catch (Exception e)
        {
            LogHelper.Error("Failed to start Neos Server", e);
        }
    }

    public void Stop() {
        if (_server != null)
        {
            _server.Stop();
            _server.Dispose();
            _server = null;
        }
    }

    public void UpdateHR(int ones, int tens, int hundreds, int hr, bool isConnected, bool isActive) {
        _neosMessage.onesHR = ones;
        _neosMessage.tensHR = tens;
        _neosMessage.hundredsHR = hundreds;
        _neosMessage.HR = hr;
        _neosMessage.isConnected = isConnected;
        _neosMessage.isActive = isActive;
        _neosMessage.HRPercent = GetHRPercent(hr);
        BroadcastMessage();
    }

    public void UpdateHeartBeat(bool isHeartBeat, bool shouldStart) {
        _neosMessage.isHRBeat = isHeartBeat;
        BroadcastMessage();
    }

    private void BroadcastMessage() {
        if (_server == null || !_server.IsListening) return;
        try {
            var msg = _neosMessage.Serialize();
            foreach (var client in _server.ListClients())
            {
                _server.SendAsync(client.Guid, msg);
            }
        }
        catch (Exception e) {
            LogHelper.Warn("Failed to broadcast message to Neos! Exception: " + e);
        }
    }

    private float GetHRPercent(float HR) {
        var targetFloat = 0f;
        var maxhr = (float)_appOptions.CurrentValue.MaxHR;
        var minhr = (float)_appOptions.CurrentValue.MinHR;
        if (HR > maxhr)
            targetFloat = 1;
        else if (HR < minhr)
            targetFloat = 0;
        else
            targetFloat = (HR - minhr) / (maxhr - minhr);
        return targetFloat;
    }

    public class NeosMessage {
        public int HR;
        public float HRPercent;
        public int hundredsHR;
        public bool isActive;
        public bool isConnected;
        public bool isHRBeat;
        public int onesHR;
        public int tensHR;

        public string Serialize() {
            var msg = string.Empty;
            foreach (var fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                msg += fieldInfo.Name + "=" + fieldInfo.GetValue(this) + ",";
            return msg.TrimEnd(',');
        }
    }
}
