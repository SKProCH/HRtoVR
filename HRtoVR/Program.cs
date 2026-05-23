using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;
using HRtoVR.Infrastructure;
using HRtoVR.Infrastructure.WritableJsonConfiguration;
using HRtoVR.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HRtoVR;

internal class Program {
    internal static bool StartMinimized { get; private set; }

    [STAThread]
    public static void Main(string[] args) {
        if (args.Contains("--headless")) {
            RunHeadless();
            return;
        }

        StartMinimized = args.Contains("--tray");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void RunHeadless() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            AttachConsole(ATTACH_PARENT_PROCESS);

        AppPaths.EnsureDirectoriesExist();

        IConfiguration configuration = WritableJsonConfigurationFabric.Create(AppPaths.ConfigPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(System.IO.Path.Combine(AppPaths.LogDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var collection = new ServiceCollection();
        collection.AddLogging(loggingBuilder => {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });
        CoreServiceRegistration.Configure(collection, configuration);

        var services = collection.BuildServiceProvider();

        var hrService = services.GetRequiredService<HRService>();
        hrService.Start().GetAwaiter().GetResult();

        Log.Information("HRtoVR headless mode started. Press Ctrl+C to exit.");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        cts.Token.WaitHandle.WaitOne();

        Log.Information("Shutting down...");

        if (services is IAsyncDisposable disposable)
            disposable.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Log.CloseAndFlush();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }

    private const uint ATTACH_PARENT_PROCESS = unchecked((uint)-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);
}
