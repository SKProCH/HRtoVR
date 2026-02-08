using System;

namespace HRtoVRChat.Services;

public interface IHRService : IDisposable
{
    IObservable<IHrListener?> ActiveListener { get; }
    IObservable<bool> HasActiveGameHandle { get; }
    IObservable<int> HeartRate { get; }
    IObservable<bool> IsConnected { get; }
}
