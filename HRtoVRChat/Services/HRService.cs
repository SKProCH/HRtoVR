using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Services;

public class HRService : IHRService {
    private readonly ILogger<HRService> _logger;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IHrListener[] _hrListeners;
    private readonly IGameHandler[] _gameHandlers;
    private readonly bool[] _gameHandlerStarted;
    private readonly BehaviorSubject<IHrListener?> _activeListener = new(null);
    private readonly BehaviorSubject<bool> _hasActiveGameHandle = new(false);
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly BehaviorSubject<IReadOnlyList<IGameHandler>> _activeGameHandlers = new([]);
    private CompositeDisposable? _globalDisposable = new();
    private CompositeDisposable? _listenerDisposable;

    public HRService(
        ILogger<HRService> logger,
        IOptionsMonitor<AppOptions> appOptions,
        IEnumerable<IHrListener> hrListeners,
        IEnumerable<IGameHandler> gameHandlers) {
        _logger = logger;
        _appOptions = appOptions;
        _hrListeners = hrListeners.ToArray();
        _gameHandlers = gameHandlers.ToArray();
        _gameHandlerStarted = new bool[_gameHandlers.Length];
    }

    public void Start() {
        var optionsChangeDisposable = _appOptions.OnChange(options => ChangeListenerIfNeeded(options.ActiveListener));
        _globalDisposable!.Add(optionsChangeDisposable ?? Disposable.Empty);

        ChangeListenerIfNeeded(_appOptions.CurrentValue.ActiveListener);

        for (var i = 0; i < _gameHandlers.Length; i++) {
            var gameHandler = _gameHandlers[i];
            if (!_appOptions.CurrentValue.GameHandlers.TryGetValue(gameHandler.Name, out var enabled) || enabled) {
                gameHandler.Start();
                _gameHandlerStarted[i] = true;
            }
        }

        _ = Task.Run(PollGameHandlers);
    }

    private void ChangeListenerIfNeeded(string? newListenerName) {
        if (_activeListener.Value?.Name == newListenerName) {
            return;
        }

        _listenerDisposable?.Dispose();
        _listenerDisposable = null;

        _activeListener.Value?.Stop();
        var newListener = _hrListeners.FirstOrDefault(x =>
            x.Name.Equals(newListenerName, StringComparison.OrdinalIgnoreCase));
        _activeListener.OnNext(newListener);

        if (newListener == null) return;
        _listenerDisposable = new CompositeDisposable();
        _listenerDisposable.Add(newListener.HeartRate.CombineLatest(newListener.IsConnected, _activeGameHandlers)
            .Subscribe(tuple => Broadcast(tuple.First, tuple.Second, tuple.Third)));
        _listenerDisposable.Add(newListener.HeartRate.Subscribe(_heartRate));
        _listenerDisposable.Add(newListener.IsConnected.Subscribe(_isConnected));
        newListener.Start();
    }

    private static void Broadcast(int heart, bool isConnected, IReadOnlyList<IGameHandler> gameHandlers) {
        foreach (var gameHandler in gameHandlers) {
            if (!gameHandler.IsConnected) continue;
            gameHandler.Update(heart, isConnected);
        }
    }

    private async Task PollGameHandlers() {
        var state = new bool[_gameHandlers.Length];
        var active = new List<IGameHandler>(_gameHandlers.Length);

        while (_globalDisposable is not null) {
            var changed = false;
            for (var i = 0; i < _gameHandlers.Length; i++) {
                var isRunning = _gameHandlers[i].IsConnected;
                if (state[i] != isRunning) {
                    changed = true;
                    if (isRunning)
                        active.Add(_gameHandlers[i]);
                    else
                        active.Remove(_gameHandlers[i]);
                }

                state[i] = isRunning;
            }

            if (changed)
                _activeGameHandlers.OnNext(active);

            await Task.Delay(2000);
        }
    }

    public void Dispose() {
        _listenerDisposable?.Dispose();
        _listenerDisposable = null;
        for (var i = 0; i < _gameHandlers.Length; i++) {
            if (_gameHandlerStarted[i]) {
                _gameHandlers[i].Stop();
                _gameHandlerStarted[i] = false;
            }
        }

        _activeListener.Value?.Stop();
        _globalDisposable!.Dispose();
        _globalDisposable = null;
    }

    public IObservable<IHrListener?> ActiveListener => _activeListener;
    public IObservable<bool> HasActiveGameHandle => _hasActiveGameHandle;
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
}