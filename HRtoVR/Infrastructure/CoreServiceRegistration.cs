using HRtoVR.Configs;
using HRtoVR.GameHandlers;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Listeners.Ble;
using HRtoVR.Listeners.Fitbit;
using HRtoVR.Listeners.HrProxy;
using HRtoVR.Listeners.HypeRate;
using HRtoVR.Listeners.Pulsoid;
using HRtoVR.Listeners.PulsoidSocket;
using HRtoVR.Listeners.Stromno;
using HRtoVR.Listeners.TextFile;
using HRtoVR.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HRtoVR.Infrastructure;

public static class CoreServiceRegistration {
    public static void Configure(IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton(configuration);

        // Options infrastructure
        services.AddOptions();
        services.AddSingleton(typeof(IConfigureOptions<>), typeof(AutoConfigureFromConfigurationOptions<>));
        services.AddSingleton(typeof(IOptionsChangeTokenSource<>), typeof(AutoConfigurationChangeTokenSource<>));
        services.AddSingleton(typeof(IOptionsMonitor<>), typeof(Options.OptionsManager<>));
        services.AddSingleton(typeof(IOptionsManager<>), typeof(Options.OptionsManager<>));
        services.AddSingleton(typeof(OptionsConfigPathResolver<>), typeof(OptionsConfigPathResolver<>));
        services.ConfigureOptionsPath<AppOptions>("App");
        services.ConfigureOptionsPath<EditableAppOptions>("App");
        services.ConfigureOptionsPath<ParameterNamesOptions>("App:ParameterNames");

        // Services
        services.AddSingleton<HRService>();
        services.AddSingleton<IHRService>(provider => provider.GetRequiredService<HRService>());

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
    }
}
