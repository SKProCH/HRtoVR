using System;
using System.Reactive.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.Services;
using HRtoVRChat_OSC_SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ArgumentsViewModel : ViewModelBase
{
    [Reactive] public bool AutoStart { get; set; }
    [Reactive] public bool SkipVRCCheck { get; set; }
    [Reactive] public bool NeosBridge { get; set; }
    [Reactive] public bool UseLegacyBool { get; set; }
    [Reactive] public string OtherArgs { get; set; } = "";

    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IConfiguration _configuration;
    private readonly ITrayIconService _trayIconService;

    public ArgumentsViewModel(IOptionsMonitor<AppOptions> appOptions, IConfiguration configuration, ITrayIconService trayIconService)
    {
        _appOptions = appOptions;
        _configuration = configuration;
        _trayIconService = trayIconService;

        // Load from Config
        var config = _appOptions.CurrentValue;
        AutoStart = config.AutoStart;
        SkipVRCCheck = config.SkipVRCCheck;
        NeosBridge = config.NeosBridge;
        UseLegacyBool = config.UseLegacyBool;
        OtherArgs = config.OtherArgs;

        // Mutual exclusivity logic
        this.WhenAnyValue(x => x.AutoStart)
            .Where(x => x && SkipVRCCheck)
            .Subscribe(_ => SkipVRCCheck = false);

        this.WhenAnyValue(x => x.SkipVRCCheck)
            .Where(x => x && AutoStart)
            .Subscribe(_ => AutoStart = false);

        // Update Config logic
        this.WhenAnyValue(x => x.AutoStart, x => x.SkipVRCCheck, x => x.NeosBridge, x => x.UseLegacyBool)
            .Skip(1) // Skip initial values
            .Subscribe(_ => UpdateConfig());

        this.WhenAnyValue(x => x.OtherArgs)
            .Skip(1)
            .Subscribe(val => {
                if (_configuration != null)
                    _configuration["OtherArgs"] = val;
            });
    }

    public void SaveConfig()
    {
        if (_configuration != null)
            _configuration["OtherArgs"] = OtherArgs;
    }

    private void UpdateConfig()
    {
        if (_configuration != null)
        {
            _configuration["AutoStart"] = AutoStart.ToString();
            _configuration["SkipVRCCheck"] = SkipVRCCheck.ToString();
            _configuration["NeosBridge"] = NeosBridge.ToString();
            _configuration["UseLegacyBool"] = UseLegacyBool.ToString();
        }

        _trayIconService.Update(new TrayIconInfo {
            AutoStart = AutoStart,
            SkipVRCCheck = SkipVRCCheck,
            NeosBridge = NeosBridge
        });
    }
}
