using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace HRtoVRChat.Infrastructure;

public class StartStopServiceBase : ReactiveObject, IStartStopService {
    private CancellationTokenSource? _cts;
    private CompositeDisposable? _compositeDisposable;
    protected virtual Task Run(CompositeDisposable disposables, CancellationToken token) { return Task.CompletedTask; }

    public virtual void Start() {
        _cts = new CancellationTokenSource();
        _ = Task.Run(async () => {
            _compositeDisposable = new CompositeDisposable();
            try {
                await Run(_compositeDisposable, _cts.Token);
            }
            catch (Exception) {
                // ignored
            }
        });
    }

    public virtual void Stop() {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _compositeDisposable?.Dispose();
        _compositeDisposable = null;
    }
}