namespace HRtoVRChat.GameHandlers;

public interface IGameHandler {
    string Name { get; }
    bool IsGameRunning();
    void Init();
    void Start();
    void Stop();
    void UpdateHR(int ones, int tens, int hundreds, int hr, bool isConnected, bool isActive);
    void UpdateHeartBeat(bool isHeartBeat, bool shouldStart);
}
