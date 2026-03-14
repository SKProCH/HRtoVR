using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace HRtoVR.Infrastructure;

public static class ExtensionMethods {
    public static CancellationToken RegisterToken(this CompositeDisposable disposable) {
        ObjectDisposedException.ThrowIf(disposable.IsDisposed, disposable);
        var cts = new CancellationTokenSource();
        disposable.Add(Disposable.Create(() => cts.Cancel()));
        return cts.Token;
    }

    public static T? DisposeNullableWith<T>(this T? item, CompositeDisposable compositeDisposable)
        where T : IDisposable {
        ArgumentNullException.ThrowIfNull(compositeDisposable);

        if (item is null) {
            return item;
        }

        compositeDisposable.Add(item);
        return item;
    }

    public static Task WaitAsync(this CancellationToken cancellationToken) {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetResult());
        return tcs.Task;
    }

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout) {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
        if (completedTask == task)
            return await task;
        throw new TimeoutException();
    }

    public static CancellationToken WithTimeout(this CancellationToken token, TimeSpan timeout) {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);
        return cts.Token;
    }
}