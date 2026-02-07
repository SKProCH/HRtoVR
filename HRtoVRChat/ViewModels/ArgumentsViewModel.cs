using System;
using System.Reactive.Linq;
using HRtoVRChat.Services;
using HRtoVRChat_OSC_SDK;
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

    private readonly IConfigService _configService;
    private readonly ITrayIconService _trayIconService;

    public ArgumentsViewModel(IConfigService configService, ITrayIconService trayIconService)
    {
        _configService = configService;
        _trayIconService = trayIconService;

        // Load from Config
        AutoStart = _configService.LoadedUIConfig.AutoStart;
        SkipVRCCheck = _configService.LoadedUIConfig.SkipVRCCheck;
        NeosBridge = _configService.LoadedUIConfig.NeosBridge;
        UseLegacyBool = _configService.LoadedUIConfig.UseLegacyBool;
        OtherArgs = _configService.LoadedUIConfig.OtherArgs;

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
            .Subscribe(val => _configService.LoadedUIConfig.OtherArgs = val);
    }

    public void SaveConfig()
    {
        _configService.LoadedUIConfig.OtherArgs = OtherArgs;
        _configService.SaveConfig(_configService.LoadedUIConfig);
    }

    private void UpdateConfig()
    {
        _configService.LoadedUIConfig.AutoStart = AutoStart;
        _configService.LoadedUIConfig.SkipVRCCheck = SkipVRCCheck;
        _configService.LoadedUIConfig.NeosBridge = NeosBridge;
        _configService.LoadedUIConfig.UseLegacyBool = UseLegacyBool;

        _trayIconService.Update(new TrayIconManager.UpdateTrayIconInformation {
            AutoStart = AutoStart,
            SkipVRCCheck = SkipVRCCheck,
            NeosBridge = NeosBridge
        });

        _configService.SaveConfig(_configService.LoadedUIConfig);
    }
}
