using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WatsonWebsocket;

namespace HRtoVRChat.GameHandlers;

public class NeosHandler : IGameHandler {
    private WatsonWsServer? _server;
    private NeosMessage _neosMessage = new();
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger _logger;

    public NeosHandler(IOptionsMonitor<AppOptions> appOptions, ILogger<NeosHandler> logger)
    {
        _appOptions = appOptions;
        _logger = logger;
    }

    public bool IsRunning() {
        return Process.GetProcessesByName("Neos").Length > 0;
    }

    public void Start() {
        _server = new WatsonWsServer("127.0.0.1", 4206, false);

        try
        {
            _server?.Start();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Neos Server");
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

    public void Update(int heartBeat, bool isConnected)
    {
        // Split heartBeat into ones, tens, hundreds
        var hundreds = heartBeat / 100 % 10;
        var tens = heartBeat / 10 % 10;
        var ones = heartBeat % 10;

        _neosMessage.onesHR = ones;
        _neosMessage.tensHR = tens;
        _neosMessage.hundredsHR = hundreds;
        _neosMessage.HR = heartBeat;
        _neosMessage.isConnected = isConnected;
        _neosMessage.isActive = isConnected;
        _neosMessage.HRPercent = GetHRPercent(heartBeat);
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
            _logger.LogWarning(e, "Failed to broadcast message to Neos!");
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
