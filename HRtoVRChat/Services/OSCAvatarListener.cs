using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HRtoVRChat_OSC_SDK;

namespace HRtoVRChat.Services;

public interface IOSCAvatarListener
{
    Action<OSCAvatarListener.AvatarChangeMessage> OnAvatarChanged { get; set; }
    OSCAvatarListener.AvatarChangeMessage? CurrentAvatar { get; }
    void Init();
}

public class OSCAvatarListener : IOSCAvatarListener
{
    private readonly IOSCService _oscService;

    public Action<AvatarChangeMessage> OnAvatarChanged { get; set; } = _ => { };
    public AvatarChangeMessage? CurrentAvatar { get; private set; }

    public OSCAvatarListener(IOSCService oscService)
    {
        _oscService = oscService;
    }

    public void Init()
    {
        _oscService.OnOscMessage += message =>
        {
            var msg = message?.ToString() ?? "unknown";
            if (message != null && msg != "unknown")
            {
                switch (message?.Address)
                {
                    case "/avatar/change":
                        // Find the AvatarFile
                        try
                        {
                            var location = FindAvatarLocation((string)message.Arguments[0]);
                            if (location != string.Empty && File.Exists(location))
                            {
                                // Read the file
                                var text = File.ReadAllText(location);
                                var acm = JsonSerializer.Deserialize<AvatarChangeMessage>(text);
                                if (acm != null)
                                {
                                    CurrentAvatar = acm;
                                    OnAvatarChanged.Invoke(acm);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogHelper.Error("Failed to get avatar file!", e);
                        }

                        break;
                }
            }
        };
    }

    private string FindAvatarLocation(string id)
    {
        // Since this is just for debug, any user directory will work fine
        var fileLocation = string.Empty;
        var vrchat_osc_data_location =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/../LocalLow",
                "VRChat/VRChat/OSC");
        if (Directory.Exists(vrchat_osc_data_location))
        {
            foreach (var directory in Directory.GetDirectories(vrchat_osc_data_location))
            {
                if (Directory.Exists(Path.Combine(directory, "Avatars")))
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(directory, "Avatars")))
                    {
                        var fn = Path.GetFileNameWithoutExtension(file);
                        if (fn == id)
                            fileLocation = file;
                    }
                }
                else if (HRService.Gargs.Contains("--no-avatars-folder"))
                {
                    // check this directory to see if the files are there (for whatever reason)
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        var fn = Path.GetFileNameWithoutExtension(file);
                        if (fn == id)
                            fileLocation = file;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(fileLocation) || !File.Exists(fileLocation))
            LogHelper.Warn("No Config File Found for avatar with id " + id);
        return fileLocation;
    }

    public record AvatarChangeMessage
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<AvatarParameters> parameters { get; set; }

        public Messages.AvatarInfo ToAvatarInfo()
        {
            var ai = new Messages.AvatarInfo
            {
                id = id,
                name = name,
                parameters = new List<string>()
            };
            foreach (var avatarParameters in parameters)
                ai.parameters.Add(avatarParameters.name);
            return ai;
        }
    }

    public record AvatarParameters
    {
        public string name { get; set; }
        public AvatarInputOutput input { get; set; }
        public AvatarInputOutput output { get; set; }
    }

    public record AvatarInputOutput
    {
        public string address { get; set; }
        public string type { get; set; }
    }
}
