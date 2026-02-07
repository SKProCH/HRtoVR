using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HRtoVRChat;

public static class SoftwareManager {
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

    public static string GetLatestVersion() => "Integrated";
    public static string GetInstalledVersion() => "Integrated";
}
