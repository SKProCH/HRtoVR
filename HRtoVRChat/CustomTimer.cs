using System;
using Timer = System.Timers.Timer;

namespace HRtoVRChat;

public class CustomTimer {
    private readonly Timer _timer;

    public CustomTimer(int ms, Action<CustomTimer> callback) {
        if (_timer != null)
            Close();
        _timer = new Timer(ms);
        _timer.AutoReset = true;
        _timer.Elapsed += (sender, args) => { callback.Invoke(this); };
        _timer.Start();
        IsRunning = true;
    }

    public bool IsRunning { get; private set; }

    public void Close() {
        if (_timer != null) {
            _timer.Stop();
            _timer.Close();
        }

        IsRunning = false;
    }
}

public class ExecuteInTime {
    private readonly Timer _timer;

    public ExecuteInTime(int ms, Action<ExecuteInTime> callback) {
        if (_timer != null) {
            _timer.Stop();
            _timer.Close();
        }

        _timer = new Timer(ms);
        _timer.AutoReset = false;
        _timer.Elapsed += (sender, args) => {
            callback.Invoke(this);
            IsWaiting = false;
            _timer.Stop();
            _timer.Close();
        };
        _timer.Start();
        IsWaiting = true;
    }

    public bool IsWaiting { get; private set; }
}