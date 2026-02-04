using SuperSimpleTcp;

namespace HRtoVRChat_OSC_SDK;

public class AppBridge {
    private SimpleTcpClient? _client;
    private SimpleTcpServer? _server;

    private Thread _serverUpdateThread;
    private CancellationTokenSource cts;

    public Action<Messages.AppBridgeMessage> OnAppBridgeMessage = message => { };
    public Action OnClientDisconnect = () => { };

    public bool IsServerRunning {
        get => _server != null && _server is { IsListening: true };
    }

    public bool IsClientRunning {
        get => _client != null;
    }

    public bool IsClientConnected {
        get => IsClientRunning && _client.IsConnected;
    }

    public void InitServer(Func<Messages.AppBridgeMessage?> GetData) {
        _server = new SimpleTcpServer("127.0.0.1:9001");
        _server.Start();
        cts = new CancellationTokenSource();
        _serverUpdateThread = new Thread(() => {
            while (!cts.IsCancellationRequested) {
                try {
                    var hrm = GetData.Invoke();
                    if (hrm != null) {
                        foreach (var client in _server.GetClients())
                            _server.Send(client, hrm.Serialize());
                    }
                }
                catch (Exception) { }

                Thread.Sleep(1000);
            }
        });
        _serverUpdateThread.Start();
    }

    public void StopServer() {
        if (IsServerRunning) {
            _server.Stop();
            cts.Cancel();
        }

        _server = null;
    }

    public void InitClient() {
        try {
            _client = new SimpleTcpClient("127.0.0.1:9001");
            _client.Events.DataReceived += (sender, e) => {
                try {
                    var data = e.Data;
                    var fakeDeserialize = Messages.DeserializeMessage(data);
                    var messageType = Messages.GetMessageType(fakeDeserialize);
                    switch (messageType) {
                        case "AppBridgeMessage":
                            OnAppBridgeMessage.Invoke(Messages.DeserializeMessage<Messages.AppBridgeMessage>(data));
                            break;
                    }
                }
                catch (Exception) {
                }
            };
            _client.Events.Disconnected += (sender, args) => OnClientDisconnect.Invoke();
            _client.Connect();
        }
        catch (Exception) {
            OnClientDisconnect.Invoke();
        }
    }

    public void StopClient() {
        if (IsClientRunning)
            _client.Disconnect();
        _client = null;
    }
}