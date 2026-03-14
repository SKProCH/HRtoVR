using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRtoVR.Listeners.Ble;

public static class BleExtensions {
    public static async Task<T> RetryWithDelayAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxRetries = 3,
        int delayMs = 1000,
        CancellationToken cancellationToken = default) {
        for (var i = 0; i < maxRetries; i++) {
            try {
                return await action(cancellationToken);
            }
            catch (Exception) when (i < maxRetries - 1 && !cancellationToken.IsCancellationRequested) {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        return await action(cancellationToken);
    }
}