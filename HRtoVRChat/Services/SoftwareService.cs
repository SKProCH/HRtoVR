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
    string Version { get; }
    bool IsSoftwareRunning { get; }

    void StartSoftware();
    void StopSoftware();
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

    public string LocalDirectory => App.LocalDirectory;
    public string OutputPath => App.OutputPath;
    public string Version => "Integrated";
    public bool IsSoftwareRunning { get; private set; }

    public void StartSoftware()
    {
        if (!IsSoftwareRunning) {
            try {
                // Start Service
                IsSoftwareRunning = true;
                Task.Run(async () => {
                    try {
                        await _hrService.StartAsync();
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
}
