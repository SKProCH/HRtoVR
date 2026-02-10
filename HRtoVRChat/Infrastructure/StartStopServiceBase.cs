using System;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace HRtoVRChat.Infrastructure;

public class StartStopServiceBase : ReactiveObject, IStartStopService {
    private CancellationTokenSource? _cts;
    protected virtual Task Run(CancellationToken token) { return Task.CompletedTask; }

    public virtual void Start() {
        _cts = new CancellationTokenSource();
        _ = Task.Run(async () => {
            try
            {
                await Run(_cts.Token);
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
    }
}