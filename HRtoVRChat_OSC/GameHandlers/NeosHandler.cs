using System.Diagnostics;
using System.Reflection;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace HRtoVRChat_OSC.GameHandlers;

public class NeosHandler : IGameHandler {
    public static Action<string> OnCommand = s => { };
    private WebSocketServer _server;
    private CancellationTokenSource _cts;
    private Thread _worker;
    private NeosMessage _neosMessage = new();

    public string Name => "Neos";

    public bool IsGameRunning() {
        return Process.GetProcessesByName("Neos").Length > 0;
    }

    public void Init() {
        _server = new WebSocketServer(4206);
        _server.AddWebSocketService<NeosSocketBehavior>("/HRtoVRChat");
    }

    public void Start() {
        if (_server == null) Init();

        _cts = new CancellationTokenSource();
        _worker = new Thread(() => {
            _server.Start();
            while (!_cts.IsCancellationRequested) {
                Thread.Sleep(10);
            }
            _server.Stop();
        });
        _worker.Start();
    }

    public void Stop() {
        _cts?.Cancel();
        // Wait for thread?
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
            _server.WebSocketServices.Broadcast(msg);
        }
        catch (Exception e) {
            LogHelper.Warn("Failed to broadcast message to Neos! Exception: " + e);
        }
    }

    private float GetHRPercent(float HR) {
        var targetFloat = 0f;
        var maxhr = (float)ConfigManager.LoadedConfig.MaxHR;
        var minhr = (float)ConfigManager.LoadedConfig.MinHR;
        if (HR > maxhr)
            targetFloat = 1;
        else if (HR < minhr)
            targetFloat = 0;
        else
            targetFloat = (HR - minhr) / (maxhr - minhr);
        return targetFloat;
    }

    public class NeosSocketBehavior : WebSocketBehavior {
        protected override void OnMessage(MessageEventArgs messageEventArgs) {
            var msg = messageEventArgs.Data;
            OnCommand.Invoke(msg);
        }
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
