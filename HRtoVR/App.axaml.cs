using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HRtoVR.Configs;
using HRtoVR.GameHandlers;
using HRtoVR.Infrastructure.Logging;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Infrastructure.WritableJsonConfiguration;
using HRtoVR.Listeners.Ble;
using HRtoVR.Listeners.Fitbit;
using HRtoVR.Listeners.HrProxy;
using HRtoVR.Listeners.HypeRate;
using HRtoVR.Listeners.Pulsoid;
using HRtoVR.Listeners.PulsoidSocket;
using HRtoVR.Listeners.Stromno;
using HRtoVR.Listeners.TextFile;
using HRtoVR.Services;
using HRtoVR.ViewModels;
using HRtoVR.ViewModels.GameHandlers;
using HRtoVR.ViewModels.Listeners;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace HRtoVR;

public class App : Application {
    public static IServiceProvider? Services { get; private set; }

    public static string LocalDirectory {
        get {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRtoVR");
            return string.Empty;
        }
    }

    public static string OutputPath {
        get {
            if (LocalDirectory != string.Empty)
                return Path.Combine(LocalDirectory, "HRtoVR");
            return "HRtoVR";
        }
    }

    public override void OnFrameworkInitializationCompleted() {
        // Setup Configuration
        var configPath = Path.Combine(OutputPath, "config.json");

        // Create directories and files if needed
        var dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(configPath))
            File.WriteAllText(configPath, "{}");

        // Use WritableJsonConfiguration for the single config file
        IConfiguration configuration = WritableJsonConfigurationFabric.Create(configPath);

        // Setup Logging
        if (!Directory.Exists(Path.Combine(OutputPath, "Logs")))
            Directory.CreateDirectory(Path.Combine(OutputPath, "Logs"));

        var logSink = new LogSink();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Sink(logSink)
            .WriteTo.File(Path.Combine(OutputPath, "Logs", "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Setup DI
        var collection = new ServiceCollection();

        // Register Configuration
        collection.AddSingleton(configuration);

        // Register Log Sink
        collection.AddSingleton(logSink);

        // Register Logging
        collection.AddLogging(loggingBuilder => {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });

        ConfigureServices(collection, configuration);
        Services = collection.BuildServiceProvider();

        // Initialize Services
        var hrService = Services.GetRequiredService<HRService>();
        _ = hrService.Start();

        var trayIconService = Services.GetRequiredService<ITrayIconService>();
        trayIconService.Init(this);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var mainWindow = new MainWindow();
            var mainVm = Services.GetRequiredService<MainWindowViewModel>();

            // Wire up hide event
            mainVm.RequestHide += mainWindow.Hide;

            mainWindow.DataContext = mainVm;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration) {
        // Services
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<HRService>();
        services.AddSingleton<IHRService>(provider => provider.GetRequiredService<HRService>());

        // Register Options
        services.AddOptions();
        services.AddSingleton(typeof(IConfigureOptions<>), typeof(AutoConfigureFromConfigurationOptions<>));
        services.AddSingleton(typeof(IOptionsChangeTokenSource<>), typeof(AutoConfigurationChangeTokenSource<>));
        services.AddSingleton(typeof(IOptionsMonitor<>), typeof(Infrastructure.Options.OptionsManager<>));
        services.AddSingleton(typeof(IOptionsManager<>), typeof(Infrastructure.Options.OptionsManager<>));
        services.AddSingleton(typeof(OptionsConfigPathResolver<>), typeof(OptionsConfigPathResolver<>));
        services.ConfigureOptionsPath<AppOptions>("App");
        services.ConfigureOptionsPath<EditableAppOptions>("App");
        services.ConfigureOptionsPath<ParameterNamesOptions>("App:ParameterNames");

        // HR Listeners
        services.AddSingleton<BleHrListener>();
        services.AddSingleton<IHrListener>(x => x.GetRequiredService<BleHrListener>());
        services.AddSingleton<IHrListener, FitBitListener>();
        services.AddSingleton<IHrListener, HrProxyListener>();
        services.AddSingleton<IHrListener, HypeRateListener>();
        services.AddSingleton<IHrListener, PulsoidListener>();
        services.AddSingleton<IHrListener, PulsoidSocketListener>();
        services.AddSingleton<IHrListener, StromnoListener>();
        services.AddSingleton<IHrListener, TextFileListener>();

        // Game Handlers
        services.AddSingleton<IGameHandler, VrChatOscHandler>();

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