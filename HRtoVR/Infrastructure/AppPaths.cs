using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HRtoVR.Infrastructure;

public static class AppPaths {
    public static string LocalDirectory { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRtoVR")
            : string.Empty;

    public static string OutputPath { get; } =
        LocalDirectory != string.Empty
            ? Path.Combine(LocalDirectory, "HRtoVR")
            : "HRtoVR";

    public static string ConfigPath => Path.Combine(OutputPath, "config.json");

    public static string LogDirectory => Path.Combine(OutputPath, "Logs");

    public static void EnsureDirectoriesExist() {
        var configDir = Path.GetDirectoryName(ConfigPath);
        if (configDir != null && !Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);

        if (!File.Exists(ConfigPath))
            File.WriteAllText(ConfigPath, "{}");

        if (!Directory.Exists(LogDirectory))
            Directory.CreateDirectory(LogDirectory);
    }
}
