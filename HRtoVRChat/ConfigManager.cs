using System.Collections.Generic;
using System.IO;
using HRtoVRChat.Configs;
using Tommy.Serializer;

namespace HRtoVRChat;

public class ConfigManager {
    public static readonly string ConfigLocation = Path.Combine(SoftwareManager.OutputPath, "config.cfg");
    public static Config LoadedConfig { get; private set; } = new();
    public static UIConfig LoadedUIConfig { get; private set; } = new();

    public static string UIConfigLocation {
        get {
            if (!string.IsNullOrEmpty(SoftwareManager.LocalDirectory))
                return SoftwareManager.LocalDirectory + "/" + "uiconfig.cfg";
            return "uiconfig.cfg";
        }
    }

    public static void CreateConfig() {
        if (Directory.Exists(SoftwareManager.OutputPath) && File.Exists(ConfigLocation)) {
            // Load
            var nc = TommySerializer.FromTomlFile<Config>(ConfigLocation) ?? new Config();
            //SaveConfig(nc);
            LoadedConfig = nc;
        }
        else
            LoadedConfig = new Config();

        if (File.Exists(UIConfigLocation)) {
            // Load
            var nuic = TommySerializer.FromTomlFile<UIConfig>(UIConfigLocation) ?? new UIConfig();
            //SaveConfig(nc);
            LoadedUIConfig = nuic;
        }
        else
            LoadedUIConfig = new UIConfig();
    }

    public static void SaveConfig(Config config) {
        var dir = Path.GetDirectoryName(ConfigLocation);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        TommySerializer.ToTomlFile(config, ConfigLocation);
    }

    public static void SaveConfig(UIConfig uiConfig) {
        TommySerializer.ToTomlFile(uiConfig, UIConfigLocation);
    }
}

[TommyTableName("HRtoVRChat")]
public class Config {
    [TommyComment("Allow HRtoVRChat to be used with ChilloutVR. Requires an OSC mod for ChilloutVR")] [TommyInclude]
    public bool ExpandCVR = true;

    [TommyInclude]
    public FitbitConfig FitbitConfig = new();

    [TommyInclude]
    public HRProxyConfig HRProxyConfig = new();

    [TommyComment("The source from where to pull Heart Rate Data")] [TommyInclude]
    public string hrType = "unknown";

    [TommyInclude]
    public HypeRateConfig HypeRateConfig = new();

    [TommyComment("The IP to send messages to")] [TommyInclude]
    public string ip = "127.0.0.1";

    [TommyComment("The maximum HR for HRPercent")] [TommyInclude]
    public double MaxHR = 255;

    [TommyComment("The minimum HR for HRPercent")] [TommyInclude]
    public double MinHR = 0;

    [TommyComment(
        "A dictionary containing what names to use for default parameters. DON'T CHANGE THE KEYS, CHANGE THE VALUES!")]
    [TommyInclude]
    public Dictionary<string, string> ParameterNames = new() {
        ["onesHR"] = "onesHR",
        ["tensHR"] = "tensHR",
        ["hundredsHR"] = "hundredsHR",
        ["isHRConnected"] = "isHRConnected",
        ["isHRActive"] = "isHRActive",
        ["isHRBeat"] = "isHRBeat",
        ["HRPercent"] = "HRPercent",
        ["FullHRPercent"] = "FullHRPercent",
        ["HR"] = "HR"
    };

    [TommyComment("The Port to send messages to")] [TommyInclude]
    public int port = 9000;

    [TommyInclude]
    public PulsoidSocketConfig PulsoidSocketConfig = new();

    [TommyInclude]
    public PulsoidConfig PulsoidConfig = new();

    [TommyComment("The Port to receive messages from")] [TommyInclude]
    public int receiverPort = 9001;

    [TommyInclude]
    public StromnoConfig StromnoConfig = new();

    [TommyInclude]
    public TextFileConfig TextFileConfig = new();

    public static bool DoesConfigExist() {
        return File.Exists(ConfigManager.ConfigLocation);
    }
}

[TommyTableName("HRtoVRChat")]
public class UIConfig {
    [TommyComment("Automatically Start HRtoVRChat when VRChat is detected")] [TommyInclude]
    public bool AutoStart;

    [TommyComment("Broadcast data over a WebSocket designed for Neos")] [TommyInclude]
    public bool NeosBridge;

    [TommyInclude] public string OtherArgs = "";

    [TommyComment("Force HRtoVRChat to run whether or not VRChat is detected")] [TommyInclude]
    public bool SkipVRCCheck;

    [TommyComment("Cast Parameter Bools to Floats")] [TommyInclude]
    public bool UseLegacyBool;
}
