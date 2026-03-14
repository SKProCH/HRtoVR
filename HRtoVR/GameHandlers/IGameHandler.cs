using System.ComponentModel;
using HRtoVRChat.Infrastructure;

namespace HRtoVRChat.GameHandlers;

public interface IGameHandler : IStartStopService, INotifyPropertyChanged {
    string Name { get; }
    bool IsConnected { get; }
    void Update(int heartBeat, float heartBeatPercentage, bool isConnected);
}
