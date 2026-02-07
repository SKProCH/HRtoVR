using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HRtoVRChat;

public static class SoftwareManager {
    public static Action<int, int> RequestUpdateProgressBars = (x, y) => { };

    // UI Interaction Delegates
    public static Func<string, string, Task<bool>>? RequestConfirmation;
    public static Action<string, string?, bool>? ShowMessage;

    public static string LocalDirectory {
        get {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRtoVRChat");
            return string.Empty;
        }
    }

    public static string OutputPath {
        get {
            if (LocalDirectory != string.Empty)
                return Path.Combine(LocalDirectory, "HRtoVRChat");
            return "HRtoVRChat";
        }
    }

    public static bool IsInstalled => true; // Embedded

    public static bool IsSoftwareRunning { get; set; }

    public static string GetLatestVersion() => "Integrated";
    public static string GetInstalledVersion() => "Integrated";
}
