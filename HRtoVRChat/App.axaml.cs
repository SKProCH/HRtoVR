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

        ConfigureServices(collection);
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

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<IOSCService, OSCService>();
        services.AddSingleton<IParamsService, ParamsService>();
        services.AddSingleton<IOSCAvatarListener, OSCAvatarListener>();
        services.AddSingleton<ISoftwareService, SoftwareService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();
        services.AddSingleton<IHRService, HRService>();
        services.AddSingleton<IBrowserService, BrowserService>();
        services.AddSingleton<HRtoVRChat_OSC_SDK.IAppBridge, HRtoVRChat_OSC_SDK.AppBridge>();

        // HR Managers & Factory
        services.AddSingleton<Factories.IHRManagerFactory, Factories.HRManagerFactory>();
        services.AddTransient<FitBitListener>();
        services.AddTransient<HrProxyListener>();
        services.AddTransient<HypeRateListener>();
        services.AddTransient<PulsoidListener>();
        services.AddTransient<PulsoidSocketListener>();
        services.AddTransient<TextFileListener>();
        services.AddTransient<SdkListener>();

        // Game Handlers
        services.AddSingleton<IGameHandler, GameHandlers.VRChatOSCHandler>();
        services.AddSingleton<IGameHandler, GameHandlers.NeosHandler>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<HomeViewModel>();

        services.AddSingleton<ProgramViewModel>();
        services.AddSingleton<ConfigViewModel>();
        services.AddSingleton<IncomingDataViewModel>();
        services.AddSingleton<ArgumentsViewModel>();
        services.AddSingleton<ParameterNamesViewModel>();
    }
}