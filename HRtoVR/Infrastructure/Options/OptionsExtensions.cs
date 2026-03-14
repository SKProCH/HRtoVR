using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Infrastructure.Options;

public static class OptionsExtensions {
    public static IObservable<TOptions> Observe<TOptions>(this IOptionsMonitor<TOptions> monitor)
        where TOptions : class {
        return Observable.Create<TOptions>(observer => {
            observer.OnNext(monitor.CurrentValue);
            return monitor.OnChange(value => observer.OnNext(value)) ?? Disposable.Empty;
        });
    }

    public static Task WaitForChange<TOptions>(this IOptionsMonitor<TOptions> monitor, CancellationToken token) {
        var tcs = new TaskCompletionSource();
        var compositeDisposable = new CompositeDisposable();
        token.Register(() => {
            compositeDisposable.Dispose();
            tcs.TrySetCanceled();
        }).DisposeWith(compositeDisposable);
        monitor.OnChange(_ => {
            compositeDisposable.Dispose();
            tcs.TrySetResult();
        }).DisposeNullableWith(compositeDisposable);
        return tcs.Task;
    }

    public static IServiceCollection ConfigureOptionsPath<TOptions>(this IServiceCollection services, string path)
        where TOptions : class {
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(provider =>
            new ConfigurationChangeTokenSource<TOptions>(Microsoft.Extensions.Options.Options.DefaultName,
                provider.GetRequiredService<IConfiguration>().GetSection(path)));
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
            new NamedConfigureFromConfigurationOptions<TOptions>(Microsoft.Extensions.Options.Options.DefaultName,
                provider.GetRequiredService<IConfiguration>().GetSection(path), _ => { }));
        services.AddSingleton(new OptionsConfigPathResolver<TOptions>(path));
        return services;
    }
}