namespace HRtoVRChat.GameHandlers;

public interface IGameHandler {
    bool IsRunning();
    void Start();
    void Stop();
    void Update(int heartBeat, bool isConnected);
}
