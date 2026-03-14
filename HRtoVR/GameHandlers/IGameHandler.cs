using System.ComponentModel;
using HRtoVR.Infrastructure;

namespace HRtoVR.GameHandlers;

public interface IGameHandler : IStartStopService, INotifyPropertyChanged {
    string Name { get; }
    bool IsConnected { get; }
    void Update(int heartBeat, float heartBeatPercentage, bool isConnected);
}