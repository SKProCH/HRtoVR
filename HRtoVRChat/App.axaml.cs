using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Listeners;
using HRtoVRChat.Listeners.Fitbit;
using HRtoVRChat.Listeners.HrProxy;
using HRtoVRChat.Listeners.HypeRate;
using HRtoVRChat.Listeners.Pulsoid;
using HRtoVRChat.Listeners.PulsoidSocket;
using HRtoVRChat.Listeners.Stromno;
using HRtoVRChat.Listeners.TextFile;
using HRtoVRChat.Services;
using HRtoVRChat.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using WritableJsonConfiguration;

namespace HRtoVRChat;

public class App : Application {
    public IServiceProvider? Services { get; private set; }

    public static string LocalDirectory {
        get {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRtoVRChat");
            return string.Empty;
        }
    }

    public static string OutputPath {
        get {
            if (LocalDirectory != string.Empty)
                return Path.Combine(LocalDirectory, "HRtoVRChat");
            return "HRtoVRChat";
        }
    }

    public override void Initialize() {
        // Cache any assets
        AssetTools.Init();
        // Continue
        AvaloniaXamlLoader.Load(this);
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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(OutputPath, "Logs", "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Setup DI
        var collection = new ServiceCollection();

        // Register Configuration
        collection.AddSingleton(configuration);
        collection.Configure<AppOptions>(configuration);

        // Register Logging
        collection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });

        ConfigureServices(collection, configuration);
        Services = collection.BuildServiceProvider();

        // Initialize Services
        var softwareService = Services.GetRequiredService<ISoftwareService>();

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

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Services
        services.AddSingleton<IOSCService, OSCService>();
        services.AddSingleton<IParamsService, ParamsService>();
        services.AddSingleton<ISoftwareService, SoftwareService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IHRService, HRService>();
        services.AddSingleton<IBrowserService, BrowserService>();

        // Register Options
        services.Configure<FitbitOptions>(configuration.GetSection("FitbitOptions"));
        services.Configure<HRProxyOptions>(configuration.GetSection("HRProxyOptions"));
        services.Configure<HypeRateOptions>(configuration.GetSection("HypeRateOptions"));
        services.Configure<PulsoidOptions>(configuration.GetSection("PulsoidOptions"));
        services.Configure<PulsoidSocketOptions>(configuration.GetSection("PulsoidSocketOptions"));
        services.Configure<StromnoOptions>(configuration.GetSection("StromnoOptions"));
        services.Configure<TextFileOptions>(configuration.GetSection("TextFileOptions"));

        // HR Listeners
        services.AddSingleton<IHrListener, FitBitListener>();
        services.AddSingleton<IHrListener, HrProxyListener>();
        services.AddSingleton<IHrListener, HypeRateListener>();
        services.AddSingleton<IHrListener, PulsoidListener>();
        services.AddSingleton<IHrListener, PulsoidSocketListener>();
        services.AddSingleton<IHrListener, StromnoListener>();
        services.AddSingleton<IHrListener, TextFileListener>();

        // Game Handlers
        services.AddSingleton<IGameHandler, VRChatOSCHandler>();
        services.AddSingleton<IGameHandler, NeosHandler>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<HomeViewModel>();

        services.AddSingleton<ProgramViewModel>();
        services.AddSingleton<ConfigViewModel>();
        services.AddSingleton<ArgumentsViewModel>();
        services.AddSingleton<ParameterNamesViewModel>();
    }
}
