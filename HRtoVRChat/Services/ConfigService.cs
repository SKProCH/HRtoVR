using System.IO;
using HRtoVRChat;
using Tommy.Serializer;

namespace HRtoVRChat.Services;

public class ConfigService : IConfigService
{
    public static readonly string ConfigLocation = Path.Combine(SoftwareManager.OutputPath, "config.cfg");

    public Config LoadedConfig { get; private set; } = new();
    public UIConfig LoadedUIConfig { get; private set; } = new();

    public static string UIConfigLocation {
        get {
            if (!string.IsNullOrEmpty(SoftwareManager.LocalDirectory))
                return SoftwareManager.LocalDirectory + "/" + "uiconfig.cfg";
            return "uiconfig.cfg";
        }
    }

    public void CreateConfig() {
        if (Directory.Exists(SoftwareManager.OutputPath) && File.Exists(ConfigLocation)) {
            // Load
            var nc = TommySerializer.FromTomlFile<Config>(ConfigLocation) ?? new Config();
            LoadedConfig = nc;
        }
        else
            LoadedConfig = new Config();

        if (File.Exists(UIConfigLocation)) {
            // Load
            var nuic = TommySerializer.FromTomlFile<UIConfig>(UIConfigLocation) ?? new UIConfig();
            LoadedUIConfig = nuic;
        }
        else
            LoadedUIConfig = new UIConfig();
    }

    public void SaveConfig(Config config) {
        var dir = Path.GetDirectoryName(ConfigLocation);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        TommySerializer.ToTomlFile(config, ConfigLocation);
        LoadedConfig = config;
    }

    public void SaveConfig(UIConfig uiConfig) {
        TommySerializer.ToTomlFile(uiConfig, UIConfigLocation);
        LoadedUIConfig = uiConfig;
    }

    public bool DoesConfigExist() {
        return File.Exists(ConfigLocation);
    }
}
