using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Services;

public class HRService : IHRService
{
    private readonly ILogger<HRService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IOSCService _oscService;
    private readonly IParamsService _paramsService;
    private readonly IEnumerable<IHrListener> _hrListeners;
    private readonly IEnumerable<IGameHandler> _injectedGameHandlers;

    private IHrListener? activeHRManager;
    private bool isRestarting;

    private List<IGameHandler> _gameHandlers = new();

    private CustomTimer? loopCheck;

    private Task? VerifyVRCOpen;
    private CancellationTokenSource vvoToken = new();

    private CompositeDisposable _subscriptions = new();
    private int _lastHR;
    private bool _lastIsConnected;

    public HRService(
        ILogger<HRService> logger,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<AppOptions> appOptions,
        IOSCService oscService,
        IParamsService paramsService,
        IEnumerable<IHrListener> hrListeners,
        IEnumerable<IGameHandler> gameHandlers)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _appOptions = appOptions;
        _oscService = oscService;
        _paramsService = paramsService;
        _hrListeners = hrListeners;
        _injectedGameHandlers = gameHandlers;
    }

    public async Task StartAsync()
    {
        // _configService.CreateConfig(); // Handled by DI/App.axaml.cs
        _oscService.Init();

        // Initialize Game Handlers
        _gameHandlers.Clear();

        foreach (var handler in _injectedGameHandlers)
        {
            if (handler is VRChatOSCHandler)
            {
                _gameHandlers.Add(handler);
            }
            else if (handler is NeosHandler neosHandler)
            {
                if (_appOptions.CurrentValue.NeosBridge)
                {
                    _logger.LogInformation("Enabling NeosBridge Handler");
                    _gameHandlers.Add(handler);
                }
            }
        }

        var foundOnStart = _gameHandlers.Any(gh => gh.IsRunning());

        if (foundOnStart)
        {
            await Check();
        }
        else
        {
            if (_appOptions.CurrentValue.AutoStart)
            {
                _logger.LogInformation("No supported game found! Waiting...");
                loopCheck = new CustomTimer(5000, async ct => await LoopCheck());
            }
            else
            {
                await Check();
            }
        }
    }

    private async Task LoopCheck()
    {
        var foundGame = _gameHandlers.Any(gh => gh.IsRunning());
        if (foundGame)
        {
            _logger.LogInformation("Found Game! Starting...");
            await Check();
        }
    }

    private async Task Check()
    {
        var fromAutoStart = _appOptions.CurrentValue.AutoStart;
        var gameRunning = _gameHandlers.Any(gh => gh.IsRunning());

        if (gameRunning || _appOptions.CurrentValue.SkipVRCCheck)
        {
            if (loopCheck?.IsRunning ?? false)
                loopCheck.Close();
            await StartInternal();
        }
        else
        {
            if (fromAutoStart)
            {
                loopCheck?.Close();
                loopCheck = new CustomTimer(5000, async ct => await LoopCheck());
            }

            // Save all logs to file - Handled by Serilog File Sink
            // Exit
            _logger.LogWarning("No supported game was detected!");
        }
    }

    private async Task StartInternal()
    {
        if (!_appOptions.CurrentValue.SkipVRCCheck)
        {
            vvoToken = new CancellationTokenSource();
            var token = vvoToken.Token;
            VerifyVRCOpen = Task.Run(async () =>
            {
                var isOpen = _gameHandlers.Any(gh => gh.IsRunning());
                while (!token.IsCancellationRequested)
                {
                    isOpen = _gameHandlers.Any(gh => gh.IsRunning());
                    if (!isOpen)
                        vvoToken.Cancel();
                    try {
                        await Task.Delay(1500, token);
                    } catch (TaskCanceledException) { break; }
                }

                _logger.LogInformation("Thread Stopped");
                var fromAutoStart = _appOptions.CurrentValue.AutoStart;
                Stop(!fromAutoStart, fromAutoStart);
            }, token);
        }

        // Initialize and Start Handlers
        foreach (var handler in _gameHandlers)
        {
            try
            {
                handler.Start();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start handler {HandlerName}", handler.GetType().Name);
            }
        }

        // Continue
        await StartHRListenerAsync();
        _logger.LogInformation("Started");
    }

    public void Stop(bool quitApp = false, bool autoStart = false)
    {
        // Stop HR Listener
        StopHRListener();

        // Stop Handlers
        foreach (var handler in _gameHandlers)
        {
            try
            {
                handler.Stop();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to stop handler {HandlerName}", handler.GetType().Name);
            }
        }

        // Stop Extraneous Tasks
        if (loopCheck?.IsRunning ?? false)
            loopCheck.Close();

        _logger.LogInformation("Stopped");
        // Quit the App
        if (quitApp)
        {
            // Save all logs to file - handled by Serilog
            // var dt = DateTime.Now;
            // LogHelper.SaveToFile($"{dt.Hour}-{dt.Minute}-{dt.Second}-{dt.Millisecond} {dt.Day}-{dt.Month}-{dt.Year}");
            // Exit
            // Environment.Exit(0);
        }

        if (autoStart)
        {
            _logger.LogInformation("Restarting when Game Detected");
            loopCheck = new CustomTimer(5000, async ct => await LoopCheck());
        }
    }

    public void RestartHRListener()
    {
        var loops = 0;
        if (!isRestarting)
        {
            isRestarting = true;
            // Called for when you need to Reset the HRListener
            StopHRListener();
            Task.Run(async () =>
            {
                while (loops <= 2)
                {
                    await Task.Delay(1000);
                    loops++;
                }

                isRestarting = false;
                await StartHRListenerAsync(true);
            });
        }
    }

    public async Task StartHRListenerAsync(bool fromRestart = false)
    {
        // Start Manager based on Config
        var hrType = _appOptions.CurrentValue.HrType;
        // Check activeHRManager
        if (activeHRManager != null)
        {
            _logger.LogWarning("HRListener is currently active! Stop it first");
            return;
        }

        activeHRManager = _hrListeners.FirstOrDefault(x => x.Name.Equals(hrType, StringComparison.OrdinalIgnoreCase));

        if (activeHRManager == null)
        {
            _logger.LogWarning("No hrType was selected! Please see README if you think this is an error!");
            Stop(true);
            return;
        }

        if (activeHRManager.Name.Equals("Pulsoid", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "\n=========================================================================================\n" +
                "WARNING ABOUT PULSOID\n" +
                "It is detected that you're using the Pulsoid Method for grabbing HR Data,\n" +
                "Please note that this method will soon be DEPRECATED and replaced with PulsoidSocket!\n" +
                "Please see the URL below on how to upgrade!\n" +
                "https://github.com/200Tigersbloxed/HRtoVRChat_OSC/wiki/Upgrading-from-Pulsoid-to-PulsoidSocket \n" +
                "=========================================================================================\n\n" +
                "Starting Pulsoid in 25 Seconds...");
            await Task.Delay(25000);
        }

        _subscriptions = new CompositeDisposable();
        activeHRManager.HeartRate.Subscribe(hr => {
            _lastHR = hr;
            UpdateHandlers();
        }).DisposeWith(_subscriptions);

        activeHRManager.IsConnected.Subscribe(connected => {
            _lastIsConnected = connected;
            UpdateHandlers();
        }).DisposeWith(_subscriptions);

        activeHRManager.Start();
    }

    public void StopHRListener()
    {
        activeHRManager?.Stop();

        activeHRManager = null;
        _subscriptions.Dispose();
        _subscriptions = new CompositeDisposable();
    }

    private void UpdateHandlers()
    {
        // Notify Handlers
        foreach (var handler in _gameHandlers)
        {
            handler.Update(_lastHR, _lastIsConnected);
        }
    }
}
