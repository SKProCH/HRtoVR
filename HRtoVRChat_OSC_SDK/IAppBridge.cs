using System;

namespace HRtoVRChat_OSC_SDK;

public interface IAppBridge
{
    Action<Messages.AppBridgeMessage> OnAppBridgeMessage { get; set; }
    Action OnClientDisconnect { get; set; }
    bool IsServerRunning { get; }
    bool IsClientRunning { get; }
    bool IsClientConnected { get; }
    void InitServer(Func<Messages.AppBridgeMessage?> GetData);
    void StopServer();
    void InitClient();
    void StopClient();
}
