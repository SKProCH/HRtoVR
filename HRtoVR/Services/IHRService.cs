using System;

namespace HRtoVR.Services;

public interface IHRService : IAsyncDisposable {
    IObservable<IHrListener?> ActiveListener { get; }
    IObservable<bool> HasActiveGameHandle { get; }
    IObservable<int> HeartRate { get; }
    IObservable<bool> IsConnected { get; }
}