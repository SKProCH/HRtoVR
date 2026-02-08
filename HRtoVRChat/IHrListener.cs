using System;

namespace HRtoVRChat;

public interface IHrListener {
    void Start();
    string Name { get; }
    IObservable<int> HeartRate { get; }
    IObservable<bool> IsConnected { get; }
    void Stop();
}
