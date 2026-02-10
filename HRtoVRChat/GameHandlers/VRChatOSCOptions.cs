using System.ComponentModel;

namespace HRtoVRChat.GameHandlers;

public class VrChatOscOptions
{
    [Description("The IP to send messages to")]
    public string Ip { get; set; } = "localhost";

    [Description("The Port to send messages to")]
    public int Port { get; set; } = 9000;

    [Description("The Port to receive messages from")]
    public int ReceiverPort { get; set; } = 9001;

    [Description("Cast Parameter Bools to Floats")]
    public bool UseLegacyBool { get; set; }

    [Description("Allow HRtoVRChat to be used with ChilloutVR. Requires an OSC mod for ChilloutVR")]
    public bool ExpandCVR { get; set; } = true;
}
