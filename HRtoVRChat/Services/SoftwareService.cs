using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRtoVRChat.Services;

public interface ISoftwareService
{
    string LocalDirectory { get; }
    string OutputPath { get; }
    bool IsInstalled { get; }
    bool IsSoftwareRunning { get; }
    Action<string, string?, bool> ShowMessage { get; set; }
    Func<string, string, Task<bool>> RequestConfirmation { get; set; }
    Action<string?, string?> OnConsoleUpdate { get; set; }
    Action<int, int> RequestUpdateProgressBars { get; set; }

    string GetLatestVersion();
    string GetInstalledVersion();
    Task InstallSoftware(Action onFinish);
    void StartSoftware();
    void StopSoftware();
    void SendCommand(string command);
}

public class SoftwareService : ISoftwareService
{
    private readonly IHRService _hrService;
    private readonly IConfigService _configService;

    public SoftwareService(IHRService hrService, IConfigService configService)
    {
        _hrService = hrService;
        _configService = configService;
        SoftwareManager.OnConsoleUpdate = (msg, color) => OnConsoleUpdate?.Invoke(msg, color);
    }

    public string LocalDirectory => SoftwareManager.LocalDirectory;
    public string OutputPath => SoftwareManager.OutputPath;
    public bool IsInstalled => SoftwareManager.IsInstalled;
    public bool IsSoftwareRunning => SoftwareManager.IsSoftwareRunning;

    public Action<string, string?, bool> ShowMessage
    {
        get => SoftwareManager.ShowMessage ?? ((_, _, _) => { });
        set => SoftwareManager.ShowMessage = value;
    }

    public Func<string, string, Task<bool>> RequestConfirmation
    {
        get => SoftwareManager.RequestConfirmation ?? ((_, _) => Task.FromResult(false));
        set => SoftwareManager.RequestConfirmation = value;
    }

    public Action<string?, string?> OnConsoleUpdate { get; set; } = (s, s1) => { };

    public Action<int, int> RequestUpdateProgressBars
    {
        get => SoftwareManager.RequestUpdateProgressBars;
        set => SoftwareManager.RequestUpdateProgressBars = value;
    }

    public string GetLatestVersion() => SoftwareManager.GetLatestVersion();
    public string GetInstalledVersion() => SoftwareManager.GetInstalledVersion();

    public async Task InstallSoftware(Action onFinish)
    {
        // Stubs for compatibility with ViewModel calls
        ShowMessage?.Invoke("HRtoVRChat", "The backend is now integrated and does not need installation.", false);
        onFinish?.Invoke();
        await Task.CompletedTask;
    }

    private string[] GetArgs() {
        List<string> Args = new();
        if (_configService.LoadedUIConfig != null) {
            if (_configService.LoadedUIConfig.AutoStart)
                Args.Add("--auto-start");
            if (_configService.LoadedUIConfig.SkipVRCCheck)
                Args.Add("--skip-vrc-check");
            if (_configService.LoadedUIConfig.NeosBridge)
                Args.Add("--neos-bridge");
            if (_configService.LoadedUIConfig.UseLegacyBool)
                Args.Add("--use-01-bool");
            try {
                if (!string.IsNullOrEmpty(_configService.LoadedUIConfig.OtherArgs))
                    foreach (var s in _configService.LoadedUIConfig.OtherArgs.Split(' '))
                        Args.Add(s);
            }
            catch (Exception) { }
        }

        return Args.ToArray();
    }

    public void StartSoftware()
    {
        if (!IsSoftwareRunning) {
            try {
                // Subscribe to Logs
                LogHelper.OnLog -= HandleLog;
                LogHelper.OnLog += HandleLog;

                // Start Service
                SoftwareManager.IsSoftwareRunning = true;
                Task.Run(() => {
                    try {
                        _hrService.Start(GetArgs());
                    } catch (Exception e) {
                        OnConsoleUpdate?.Invoke($"CRITICAL ERROR: {e.Message}\n{e.StackTrace}", "Red");
                        SoftwareManager.IsSoftwareRunning = false;
                    }
                });
            }
            catch (Exception e) {
                ShowMessage?.Invoke("HRtoVRChat", "Failed to start service: " + e.Message, true);
                SoftwareManager.IsSoftwareRunning = false;
            }
        }
    }

    public void StopSoftware()
    {
        if (IsSoftwareRunning) {
            try {
                _hrService.Stop();
                SoftwareManager.IsSoftwareRunning = false;
                LogHelper.OnLog -= HandleLog;
            }
            catch (Exception) { }
        }
    }

    public void SendCommand(string command)
    {
        if (IsSoftwareRunning) {
            try {
                OnConsoleUpdate?.Invoke("> " + command, "Purple");
                _hrService.HandleCommand(command);
            }
            catch (Exception) {
                ShowMessage?.Invoke("HRtoVRChat", "Failed to send command due to an error!", true);
            }
        }
    }

    private void HandleLog(string msg, LogHelper.LogLevel level) {
        string color = level switch {
            LogHelper.LogLevel.Warn => "Yellow",
            LogHelper.LogLevel.Error => "Red",
            LogHelper.LogLevel.Debug => "Gray",
            _ => "White"
        };
        OnConsoleUpdate?.Invoke(msg, color);
    }
}

