using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Services;

public interface ISoftwareService
{
    string LocalDirectory { get; }
    string OutputPath { get; }
    bool IsInstalled { get; }
    bool IsSoftwareRunning { get; }
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
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly ILogger<SoftwareService> _logger;

    public SoftwareService(IHRService hrService, IOptionsMonitor<AppOptions> appOptions, ILogger<SoftwareService> logger)
    {
        _hrService = hrService;
        _appOptions = appOptions;
        _logger = logger;
    }

    public string LocalDirectory => SoftwareManager.LocalDirectory;
    public string OutputPath => SoftwareManager.OutputPath;
    public bool IsInstalled => SoftwareManager.IsInstalled;
    public bool IsSoftwareRunning { get; private set; }

    public Action<int, int> RequestUpdateProgressBars { get; set; } = (_, _) => { };

    public string GetLatestVersion() => SoftwareManager.GetLatestVersion();
    public string GetInstalledVersion() => SoftwareManager.GetInstalledVersion();

    public async Task InstallSoftware(Action onFinish)
    {
        // Stubs for compatibility with ViewModel calls
        _logger.LogInformation("The backend is now integrated and does not need installation.");
        onFinish?.Invoke();
        await Task.CompletedTask;
    }

    public void StartSoftware()
    {
        if (!IsSoftwareRunning) {
            try {
                // Start Service
                IsSoftwareRunning = true;
                Task.Run(() => {
                    try {
                        _hrService.Start();
                    } catch (Exception e) {
                        _logger.LogError(e, "CRITICAL ERROR: {Message}", e.Message);
                        IsSoftwareRunning = false;
                    }
                });
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to start service: {Message}", e.Message);
                IsSoftwareRunning = false;
            }
        }
    }

    public void StopSoftware()
    {
        if (IsSoftwareRunning) {
            try {
                _hrService.Stop();
                IsSoftwareRunning = false;
            }
            catch (Exception) { }
        }
    }

    public void SendCommand(string command)
    {
        if (IsSoftwareRunning) {
            try {
                _logger.LogInformation("> {Command}", command);
                _hrService.HandleCommand(command);
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to send command due to an error!");
            }
        }
    }
}
