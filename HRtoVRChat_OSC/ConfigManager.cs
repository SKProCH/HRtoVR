using Tommy.Serializer;

namespace HRtoVRChat_OSC;

public class ConfigManager {
    public static string ConfigLocation = "config.cfg";
    public static Config LoadedConfig { get; private set; } = new Config();

    public static void CreateConfig() {
        if (File.Exists(ConfigLocation)) {
            // Load
            LogHelper.Log("Loading Config.");
            var nc = TommySerializer.FromTomlFile<Config>(ConfigLocation) ?? new Config();
            SaveConfig(nc);
            LoadedConfig = nc;
        }
        else {
            // Create
            LogHelper.Log("No Config Found! Creating Config.");
            var nc = new Config();
            SaveConfig(nc);
            LoadedConfig = nc;
        }

        LogHelper.Log("Loaded Config!");
    }

    public static void SaveConfig(Config config) {
        TommySerializer.ToTomlFile(config, ConfigLocation);
    }
}

[TommyTableName("HRtoVRChat_OSC")]
public class Config {
    [TommyComment("Allow HRtoVRChat_OSC to be used with ChilloutVR. Requires an OSC mod for ChilloutVR")] [TommyInclude]
    public bool ExpandCVR = true;

    [TommyComment("(FitbitHRtoWS Only) The WebSocket to listen to data")] [TommyInclude]
    public string fitbitURL = "ws://localhost:8080/";

    [TommyComment("(HRProxy Only) The code to pull HRProxy Data from")] [TommyInclude]
    public string hrproxyId = string.Empty;

    [TommyComment("The source from where to pull Heart Rate Data")] [TommyInclude]
    public string hrType = "unknown";

    [TommyComment("(HypeRate Only) The code to pull HypeRate Data from")] [TommyInclude]
    public string hyperateSessionId = string.Empty;

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

    [TommyComment("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")] [TommyInclude]
    public string pulsoidkey = string.Empty;

    [TommyComment("(Pulsoid Only) The widgetId to pull HeartRate Data from Pulsoid")] [TommyInclude]
    public string pulsoidwidget = string.Empty;

    [TommyComment("The Port to receive messages from")] [TommyInclude]
    public int receiverPort = 9001;

    [TommyComment("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")] [TommyInclude]
    public string stromnowidget = string.Empty;

    [TommyComment("(TextFile Only) The location of the text file to pull HeartRate Data from")] [TommyInclude]
    public string textfilelocation = string.Empty;
}