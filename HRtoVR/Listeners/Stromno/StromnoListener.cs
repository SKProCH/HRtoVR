using System.Threading.Tasks;
using HRtoVRChat.Listeners.Pulsoid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoListener : PulsoidListener
{
    private readonly IOptionsMonitor<StromnoOptions> _stromnoOptions;

    public StromnoListener(ILogger<StromnoListener> logger, IOptionsMonitor<StromnoOptions> options, IOptionsMonitor<PulsoidOptions> pulsoidOptions) : base(logger, pulsoidOptions)
    {
        _stromnoOptions = options;
    }

    public override string Name => "Stromno";

    public override async Task Start()
    {
        _optionsSubscription = _stromnoOptions.OnChange(async opt =>
        {
            _logger.LogInformation("Stromno configuration changed, restarting...");
            await Stop();
            await Start();
        });
        // Stromno uses the same protocol as Pulsoid, just with a different widget ID source
        await StartConnection(_stromnoOptions.CurrentValue.Widget);
    }
}
