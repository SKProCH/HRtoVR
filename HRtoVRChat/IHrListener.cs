using System;
using HRtoVRChat.Infrastructure;

namespace HRtoVRChat;

public interface IHrListener : IStartStopService {
    string Name { get; }
    IObservable<int> HeartRate { get; }
    IObservable<bool> IsConnected { get; }
}
