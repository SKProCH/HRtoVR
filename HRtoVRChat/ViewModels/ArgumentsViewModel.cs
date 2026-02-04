using System;
using System.Reactive.Linq;
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

    public ArgumentsViewModel()
    {
        // Load from Config
        AutoStart = ConfigManager.LoadedUIConfig.AutoStart;
        SkipVRCCheck = ConfigManager.LoadedUIConfig.SkipVRCCheck;
        NeosBridge = ConfigManager.LoadedUIConfig.NeosBridge;
        UseLegacyBool = ConfigManager.LoadedUIConfig.UseLegacyBool;
        OtherArgs = ConfigManager.LoadedUIConfig.OtherArgs;

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
            .Subscribe(val => ConfigManager.LoadedUIConfig.OtherArgs = val);
    }

    public void SaveConfig()
    {
        ConfigManager.LoadedUIConfig.OtherArgs = OtherArgs;
        ConfigManager.SaveConfig(ConfigManager.LoadedUIConfig);
    }

    private void UpdateConfig()
    {
        ConfigManager.LoadedUIConfig.AutoStart = AutoStart;
        ConfigManager.LoadedUIConfig.SkipVRCCheck = SkipVRCCheck;
        ConfigManager.LoadedUIConfig.NeosBridge = NeosBridge;
        ConfigManager.LoadedUIConfig.UseLegacyBool = UseLegacyBool;

        TrayIconManager.Update(new TrayIconManager.UpdateTrayIconInformation {
            AutoStart = AutoStart,
            SkipVRCCheck = SkipVRCCheck,
            NeosBridge = NeosBridge
        });

        ConfigManager.SaveConfig(ConfigManager.LoadedUIConfig);
    }
}
