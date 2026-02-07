using System;
using HRtoVRChat.HRManagers;
using Microsoft.Extensions.DependencyInjection;

namespace HRtoVRChat.Factories;

public interface IHRManagerFactory
{
    HRManager? CreateManager(string managerName);
}

public class HRManagerFactory : IHRManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public HRManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public HRManager? CreateManager(string managerName)
    {
        return managerName.ToLower() switch
        {
            "fitbithrtows" => _serviceProvider.GetRequiredService<FitbitManager>(),
            "hrproxy" => _serviceProvider.GetRequiredService<HRProxyManager>(),
            "hyperate" => _serviceProvider.GetRequiredService<HypeRateManager>(),
            "pulsoid" => _serviceProvider.GetRequiredService<PulsoidManager>(),
            "stromno" => _serviceProvider.GetRequiredService<PulsoidManager>(), // Stromno uses PulsoidManager logic apparently?
            "pulsoidsocket" => _serviceProvider.GetRequiredService<PulsoidSocketManager>(),
            "textfile" => _serviceProvider.GetRequiredService<TextFileManager>(),
            "omnicept" => null, // Omnicept commented out
            "sdk" => _serviceProvider.GetRequiredService<SDKManager>(),
            _ => null
        };
    }
}
