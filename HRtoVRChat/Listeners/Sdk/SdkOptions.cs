using System.ComponentModel;

namespace HRtoVRChat.Configs;

public class SdkOptions
{
    [Description("(SDK Only) The address to bind the SDK Server to")]
    public string BindingAddress { get; set; } = "127.0.0.1:9000";
}
