using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using HRtoVR.Infrastructure;
using HRtoVR.Infrastructure.Logging;
using HRtoVR.Infrastructure.WritableJsonConfiguration;
using HRtoVR.Services;
using HRtoVR.ViewModels;
using HRtoVR.ViewModels.GameHandlers;
using HRtoVR.ViewModels.Listeners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HRtoVR;

public class App : Application {
    public static IServiceProvider? Services { get; private set; }

    public static string LocalDirectory => AppPaths.LocalDirectory;

    public static string OutputPath => AppPaths.OutputPath;

    public override void OnFrameworkInitializationCompleted() {
        AppPaths.EnsureDirectoriesExist();

        IConfiguration configuration = WritableJsonConfigurationFabric.Create(AppPaths.ConfigPath);

        var logSink = new LogSink();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Sink(logSink)
            .WriteTo.File(System.IO.Path.Combine(AppPaths.LogDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var collection = new ServiceCollection();
        collection.AddSingleton(logSink);
        collection.AddLogging(loggingBuilder => {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });

        CoreServiceRegistration.Configure(collection, configuration);
        ConfigureUiServices(collection);
        Services = collection.BuildServiceProvider();

        var hrService = Services.GetRequiredService<HRService>();
        _ = hrService.Start();

        var trayIconService = Services.GetRequiredService<ITrayIconService>();
        trayIconService.Init(this);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var mainWindow = new MainWindow();
            var mainVm = Services.GetRequiredService<MainWindowViewModel>();

            mainVm.RequestHide += mainWindow.Hide;

            mainWindow.DataContext = mainVm;
            trayIconService.MainWindow = mainWindow;

            if (Program.StartMinimized) {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                trayIconService.Update(new TrayIconInfo { HideApplication = true });
            } else {
                desktop.MainWindow = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureUiServices(IServiceCollection services) {
        services.AddSingleton<ITrayIconService, TrayIconService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<BleSettingsViewModel>();

        // Page ViewModels
        services.AddSingleton<ProgramViewModel>();
        services.AddSingleton<IPageViewModel>(x => x.GetRequiredService<ProgramViewModel>());

        services.AddSingleton<ListenersViewModel>();
        services.AddSingleton<IPageViewModel>(x => x.GetRequiredService<ListenersViewModel>());

        services.AddSingleton<GameHandlersViewModel>();
        services.AddSingleton<IPageViewModel>(x => x.GetRequiredService<GameHandlersViewModel>());

        services.AddSingleton<ConfigViewModel>();
        services.AddSingleton<IPageViewModel>(x => x.GetRequiredService<ConfigViewModel>());

        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<IPageViewModel>(x => x.GetRequiredService<LogsViewModel>());
    }

    public static async Task Shutdown() {
        if (Services is IAsyncDisposable disposable) {
            await disposable.DisposeAsync();
        }

        Environment.Exit(0);
    }
}
