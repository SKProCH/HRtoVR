using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HRtoVRChat_OSC;

namespace HRtoVRChat;

public static class SoftwareManager {
    public static Action<string?, string> OnConsoleUpdate = (line, color) => { };
    public static Action<int, int> RequestUpdateProgressBars = (x, y) => { };

    // UI Interaction Delegates
    public static Func<string, string, Task<bool>>? RequestConfirmation;
    public static Action<string, string, bool>? ShowMessage;

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
                return Path.Combine(LocalDirectory, "HRtoVRChat_OSC");
            return "HRtoVRChat_OSC";
        }
    }

    public static bool IsInstalled => true; // Embedded

    public static bool IsSoftwareRunning { get; private set; }

    private static string[] GetArgs() {
        List<string> Args = new();
        if (ConfigManager.LoadedUIConfig != null) {
            if (ConfigManager.LoadedUIConfig.AutoStart)
                Args.Add("--auto-start");
            if (ConfigManager.LoadedUIConfig.SkipVRCCheck)
                Args.Add("--skip-vrc-check");
            if (ConfigManager.LoadedUIConfig.NeosBridge)
                Args.Add("--neos-bridge");
            if (ConfigManager.LoadedUIConfig.UseLegacyBool)
                Args.Add("--use-01-bool");
            try {
                if (!string.IsNullOrEmpty(ConfigManager.LoadedUIConfig.OtherArgs))
                    foreach (var s in ConfigManager.LoadedUIConfig.OtherArgs.Split(' '))
                        Args.Add(s);
            }
            catch (Exception) { }
        }

        return Args.ToArray();
    }

    public static void StartSoftware() {
        if (!IsSoftwareRunning) {
            try {
                // Setup Paths
                if (!Directory.Exists(OutputPath))
                    Directory.CreateDirectory(OutputPath);

                // Point the Library Config to the GUI OutputPath
                HRtoVRChat_OSC.ConfigManager.ConfigLocation = Path.Combine(OutputPath, "config.cfg");

                // Subscribe to Logs
                LogHelper.OnLog -= HandleLog; // Ensure no double subscription
                LogHelper.OnLog += HandleLog;

                // Start Service
                IsSoftwareRunning = true;
                Task.Run(() => {
                    try {
                        HRService.Start(GetArgs());
                    } catch (Exception e) {
                        OnConsoleUpdate.Invoke($"CRITICAL ERROR: {e.Message}\n{e.StackTrace}", "Red");
                        IsSoftwareRunning = false;
                    }
                });
            }
            catch (Exception e) {
                ShowMessage?.Invoke("HRtoVRChat", "Failed to start service: " + e.Message, true);
                IsSoftwareRunning = false;
            }
        }
    }

    private static void HandleLog(string msg, LogHelper.LogLevel level) {
        string color = level switch {
            LogHelper.LogLevel.Warn => "Yellow",
            LogHelper.LogLevel.Error => "Red",
            LogHelper.LogLevel.Debug => "Gray",
            _ => "White"
        };
        OnConsoleUpdate.Invoke(msg, color);
    }

    public static void SendCommand(string command) {
        if (IsSoftwareRunning) {
            try {
                OnConsoleUpdate.Invoke("> " + command, "Purple");
                HRService.HandleCommand(command);
            }
            catch (Exception) {
                ShowMessage?.Invoke("HRtoVRChat", "Failed to send command due to an error!", true);
            }
        }
    }

    public static void StopSoftware() {
        if (IsSoftwareRunning) {
            try {
                HRService.Stop();
                IsSoftwareRunning = false;
                LogHelper.OnLog -= HandleLog;
            }
            catch (Exception) { }
        }
    }

    // Stubs for compatibility with ViewModel calls
    public static async Task InstallSoftware(Action? callback = null) {
        ShowMessage?.Invoke("HRtoVRChat", "The backend is now integrated and does not need installation.", false);
        callback?.Invoke();
        await Task.CompletedTask;
    }

    public static string GetLatestVersion() => "Integrated";
    public static string GetInstalledVersion() => "Integrated";
}
