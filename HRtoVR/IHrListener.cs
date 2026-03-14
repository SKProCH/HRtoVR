using System;
using HRtoVR.Infrastructure;

namespace HRtoVR;

public interface IHrListener : IStartStopService {
    string Name { get; }
    IObservable<int> HeartRate { get; }
    IObservable<bool> IsConnected { get; }
    Type? SettingsViewModelType => null;
}