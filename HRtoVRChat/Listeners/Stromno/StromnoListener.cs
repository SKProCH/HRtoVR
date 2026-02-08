using HRtoVRChat.Listeners.Pulsoid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoListener : PulsoidListener
{
    private readonly StromnoOptions _options;

    public StromnoListener(ILogger<StromnoListener> logger, IOptions<StromnoOptions> options) : base(logger)
    {
        _options = options.Value;
    }

    public override string Name => "Stromno";
    public override object? Settings => _options;
    public override string? SettingsSectionName => "StromnoOptions";

    public override void Start()
    {
        // Stromno uses the same protocol as Pulsoid, just with a different widget ID source
        StartConnection(_options.Widget);
    }
}
