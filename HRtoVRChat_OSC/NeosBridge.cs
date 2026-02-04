using System.Reflection;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace HRtoVRChat_OSC;

public class NeosBridge {
    public static Action<string> OnCommand = s => { };
    public static NeosMessage _neosMessage = new();

    public CancellationTokenSource cts;

    public NeosBridge() {
        cts = new CancellationTokenSource();
        var server = new WebSocketServer(4206);
        var worker = new Thread(() => {
            Program.OnHRValuesUpdated += (ones, tens, hundreds, hr, isConnected, isActive) => {
                _neosMessage.onesHR = ones;
                _neosMessage.tensHR = tens;
                _neosMessage.hundredsHR = hundreds;
                _neosMessage.HR = hr;
                _neosMessage.isConnected = isConnected;
                _neosMessage.isActive = isActive;
                _neosMessage.HRPercent = GetHRPercent(hr);
                try {
                    var msg = _neosMessage.Serialize();
                    server.WebSocketServices.Broadcast(msg);
                }
                catch (Exception e) {
                    LogHelper.Warn("Failed to broadcast message! Exception: " + e);
                }
            };
            Program.OnHeartBeatUpdate += (isHRBeat, shouldStart) => {
                _neosMessage.isHRBeat = isHRBeat;
                try {
                    var msg = _neosMessage.Serialize();
                    server.WebSocketServices.Broadcast(msg);
                }
                catch (Exception e) {
                    LogHelper.Warn("Failed to broadcast message! Exception: " + e);
                }
            };
            server.AddWebSocketService<NeosSocketBehavior>("/HRtoVRChat");
            server.Start();
            while (!cts.IsCancellationRequested) {
                Thread.Sleep(10);
            }

            server.Stop();
        });
        worker.Start();
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