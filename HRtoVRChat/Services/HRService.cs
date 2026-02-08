using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
    private readonly BehaviorSubject<IHrListener?> _activeListener = new(null);
    private readonly BehaviorSubject<bool> _hasActiveGameHandle = new(false);
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly BehaviorSubject<IReadOnlyList<IGameHandler>> _activeGameHandlers = new([]);
    private readonly CompositeDisposable _globalDisposable = new();
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
    }

    public void Start() {
        var optionsChangeDisposable = _appOptions.OnChange(options => ChangeListenerIfNeeded(options.ActiveListener));
        _globalDisposable.Add(optionsChangeDisposable ?? Disposable.Empty);

        ChangeListenerIfNeeded(_appOptions.CurrentValue.ActiveListener);

        foreach (var gameHandler in _gameHandlers) {
            // TODO: Disabling individual game handlers
            gameHandler.Start();
        }
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
            if (gameHandler.IsRunning()) {
                gameHandler.Update(heart, isConnected);
            }
        }
    }

    public void Dispose() {
        _listenerDisposable?.Dispose();
        foreach (var gameHandler in _gameHandlers)
            gameHandler.Stop();

        _activeListener.Value?.Stop();
        _globalDisposable.Dispose();
    }

    public IObservable<IHrListener?> ActiveListener => _activeListener;
    public IObservable<bool> HasActiveGameHandle => _hasActiveGameHandle;
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
}