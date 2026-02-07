using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HRtoVRChat.Configs;

namespace HRtoVRChat.Listeners;

public class StromnoListener : PulsoidListener
{
    private readonly StromnoOptions _options;

    public StromnoListener(ILogger<StromnoListener> logger, IOptions<StromnoOptions> options) : base(logger)
    {
        _options = options.Value;
    }

    public override string Name => "Stromno";

    public override void Start()
    {
        // Stromno uses the same protocol as Pulsoid, just with a different widget ID source
        StartThread(_options.Widget);
    }
}
