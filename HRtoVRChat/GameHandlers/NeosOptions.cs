using System.ComponentModel;

namespace HRtoVRChat.GameHandlers;

public class NeosOptions
{
    [Description("The Port for the Neos WebSocket server")]
    public int Port { get; set; } = 4206;
}
