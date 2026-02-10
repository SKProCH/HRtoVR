using System.ComponentModel;

namespace HRtoVRChat.GameHandlers;

public interface IGameHandler : INotifyPropertyChanged {
    string Name { get; }
    bool IsConnected { get; }
    void Start();
    void Stop();
    void Update(int heartBeat, float heartBeatPercentage, bool isConnected);
}
