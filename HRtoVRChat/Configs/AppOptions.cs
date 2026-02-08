using System.ComponentModel;
using System.IO;
using HRtoVRChat;
using HRtoVRChat.Listeners.Fitbit;
using HRtoVRChat.Listeners.HrProxy;
using HRtoVRChat.Listeners.HypeRate;
using HRtoVRChat.Listeners.Pulsoid;
using HRtoVRChat.Listeners.PulsoidSocket;
using HRtoVRChat.Listeners.Stromno;
using HRtoVRChat.Listeners.TextFile;

namespace HRtoVRChat.Configs;

public class AppOptions {
    // Main Config properties
    [Description("Allow HRtoVRChat to be used with ChilloutVR. Requires an OSC mod for ChilloutVR")]
    public bool ExpandCVR { get; set; } = true;

    [Description("The source from where to pull Heart Rate Data")]
    public string ActiveListener { get; set; } = "unknown";

    [Description("The IP to send messages to")]
    public string Ip { get; set; } = "127.0.0.1";

    [Description("The maximum HR for HRPercent")]
    public double MaxHR { get; set; } = 255;

    [Description("The minimum HR for HRPercent")]
    public double MinHR { get; set; } = 0;

    [Description("A dictionary containing what names to use for default parameters.")]
    public ParameterNamesOptions ParameterNames { get; set; } = new();

    [Description("The Port to send messages to")]
    public int Port { get; set; } = 9000;

    [Description("The Port to receive messages from")]
    public int ReceiverPort { get; set; } = 9001;

    // Sub-options
    public FitbitOptions FitbitOptions { get; set; } = new();
    public HRProxyOptions HRProxyOptions { get; set; } = new();
    public HypeRateOptions HypeRateOptions { get; set; } = new();
    public PulsoidOptions PulsoidOptions { get; set; } = new();
    public PulsoidSocketOptions PulsoidSocketOptions { get; set; } = new();
    public StromnoOptions StromnoOptions { get; set; } = new();
    public TextFileOptions TextFileOptions { get; set; } = new();

    // UI Config properties merged
    [Description("Automatically Start HRtoVRChat when VRChat is detected")]
    public bool AutoStart { get; set; }

    [Description("Broadcast data over a WebSocket designed for Neos")]
    public bool NeosBridge { get; set; }

    public string OtherArgs { get; set; } = "";

    [Description("Force HRtoVRChat to run whether or not VRChat is detected")]
    public bool SkipVRCCheck { get; set; }

    [Description("Cast Parameter Bools to Floats")]
    public bool UseLegacyBool { get; set; }

    [Description("Disables looking for avatars in the default VRChat OSC folder")]
    public bool NoAvatarsFolder { get; set; }

    public static bool DoesConfigExist() {
        return File.Exists(Path.Combine(App.OutputPath, "config.json"));
    }
}
