using HRtoVRChat.Listeners.Pulsoid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.Stromno;

public class StromnoListener : PulsoidListener
{
    private readonly IOptionsMonitor<StromnoOptions> _options;

    public StromnoListener(ILogger<StromnoListener> logger, IOptionsMonitor<StromnoOptions> options, IOptionsMonitor<PulsoidOptions> pulsoidOptions) : base(logger, pulsoidOptions)
    {
        _options = options;
    }

    public override string Name => "Stromno";
    public override object? Settings => _options.CurrentValue;
    public override string? SettingsSectionName => "StromnoOptions";

    public override void Start()
    {
        // Stromno uses the same protocol as Pulsoid, just with a different widget ID source
        StartConnection(_options.CurrentValue.Widget);
    }
}
