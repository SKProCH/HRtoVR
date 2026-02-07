using System;
using HRtoVRChat.Listeners;
using Microsoft.Extensions.DependencyInjection;

namespace HRtoVRChat.Factories;

public interface IHRManagerFactory
{
    IHrListener? CreateManager(string managerName);
}

public class HRManagerFactory : IHRManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public HRManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IHrListener? CreateManager(string managerName)
    {
        return managerName.ToLower() switch
        {
            "fitbithrtows" => _serviceProvider.GetRequiredService<FitBitListener>(),
            "hrproxy" => _serviceProvider.GetRequiredService<HrProxyListener>(),
            "hyperate" => _serviceProvider.GetRequiredService<HypeRateListener>(),
            "pulsoid" => _serviceProvider.GetRequiredService<PulsoidListener>(),
            "stromno" => _serviceProvider.GetRequiredService<PulsoidListener>(), // Stromno uses PulsoidManager logic apparently?
            "pulsoidsocket" => _serviceProvider.GetRequiredService<PulsoidSocketListener>(),
            "textfile" => _serviceProvider.GetRequiredService<TextFileListener>(),
            "omnicept" => null, // Omnicept commented out
            "sdk" => _serviceProvider.GetRequiredService<SdkListener>(),
            _ => null
        };
    }
}
