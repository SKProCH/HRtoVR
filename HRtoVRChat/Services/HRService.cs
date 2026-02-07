using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.Listeners;
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
    private bool RunHeartBeat;

    private List<IGameHandler> _gameHandlers = new();

    private CustomTimer? loopCheck;

    private Task? VerifyVRCOpen;
    private CancellationTokenSource vvoToken = new();
    private Task? BeatThread;
    private CancellationTokenSource btToken = new();

    public CustomTimer? BoopUwUTimer;


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

        bool foundOnStart = _gameHandlers.Any(gh => gh.IsGameRunning());

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
        var foundGame = _gameHandlers.Any(gh => gh.IsGameRunning());
        if (foundGame)
        {
            _logger.LogInformation("Found Game! Starting...");
            await Check();
        }
    }

    private async Task Check()
    {
        var fromAutoStart = _appOptions.CurrentValue.AutoStart;
        bool gameRunning = _gameHandlers.Any(gh => gh.IsGameRunning());

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
                var isOpen = _gameHandlers.Any(gh => gh.IsGameRunning());
                while (!token.IsCancellationRequested)
                {
                    isOpen = _gameHandlers.Any(gh => gh.IsGameRunning());
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
                handler.Init();
                handler.Start();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start handler {HandlerName}", handler.Name);
            }
        }

        // Continue
        await StartHRListenerAsync();
        // Start Coroutine
        BoopUwUTimer = new CustomTimer(1000, ct => BoopUwU());
        _logger.LogInformation("Started");
    }

    public void Stop(bool quitApp = false, bool autoStart = false)
    {
        // Stop Everything
        if (activeHRManager != null)
        {
            try
            {
                BoopUwUTimer?.Close();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to stop ActiveHRManager");
            }
        }

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
                _logger.LogError(e, "Failed to stop handler {HandlerName}", handler.Name);
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
            if (activeHRManager.IsActive())
            {
                _logger.LogWarning("HRListener is currently active! Stop it first");
                return;
            }
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

        activeHRManager.Start();

        // Set Logger for the created manager - No longer needed as it is injected

        // Start HeartBeats if there was a valid choice
        if (!RunHeartBeat)
        {
            if (BeatThread != null && !BeatThread.IsCompleted)
            {
                try
                {
                    btToken.Cancel();
                }
                catch (Exception) { }

                RunHeartBeat = false;
            }

            btToken = new CancellationTokenSource();
            BeatThread = Task.Run(async () =>
            {
                _logger.LogInformation("Starting Beating!");
                RunHeartBeat = true;
                await HeartBeat();
            }, btToken.Token);
        }
        else if (activeHRManager == null)
            _logger.LogWarning("Can't start beat as ActiveHRManager is null!");
    }

    public void StopHRListener()
    {
        if (activeHRManager != null)
        {
            if (!activeHRManager.IsActive())
            {
                _logger.LogWarning("HRListener is currently inactive! Attempting to stop anyways.");
                //return;
            }

            activeHRManager.Stop();
        }

        activeHRManager = null;
        // Stop Beating
        if (BeatThread != null && !BeatThread.IsCompleted)
        {
            try
            {
                btToken.Cancel();
            }
            catch (Exception) { }

            RunHeartBeat = false;
        }
    }

    // why did i name the ienumerator this and why haven't i changed it
    private void BoopUwU()
    {
        var chs = new currentHRSplit();
        if (activeHRManager != null)
        {
            var HR = activeHRManager.GetHR();
            var isOpen = activeHRManager.IsOpen();
            var isActive = activeHRManager.IsActive();
            // Cast to currentHRSplit
            chs = intToHRSplit(HR);

            // Notify Handlers
            foreach (var handler in _gameHandlers)
            {
                handler.UpdateHR(chs.ones, chs.tens, chs.hundreds, HR, isOpen, isActive);
            }
        }
        else
        {
            // Notify Handlers of zero
            foreach (var handler in _gameHandlers)
            {
                handler.UpdateHR(0, 0, 0, 0, false, false);
            }
        }
    }

    private async Task HeartBeat()
    {
        var waited = false;
        var token = btToken.Token;
        while (!token.IsCancellationRequested)
        {
            if (!RunHeartBeat)
                btToken.Cancel();
            else
            {
                if (activeHRManager != null)
                {
                    var io = activeHRManager.IsOpen();
                    // This should be started by the Melon Update void
                    if (io)
                    {
                        // Get HR
                        float HR = activeHRManager.GetHR();
                        if (HR > 0)
                        {
                            if (waited)
                            {
                                _logger.LogInformation("Found ActiveHRManager! Starting HeartBeat.");
                                waited = false;
                            }

                            // HeartBeat OFF
                            foreach (var handler in _gameHandlers)
                            {
                                handler.UpdateHeartBeat(false, false);
                            }

                            // Calculate wait interval
                            var waitTime = default(float);
                            // When lowering the HR significantly, this will cause issues with the beat bool
                            // Dubbed the "Breathing Exercise" bug
                            // There's a 'temp' fix for it right now, but I'm not sure how it'll hold up
                            try { waitTime = 1 / ((HR - 0.2f) / 60); }
                            catch (Exception)
                            {
                                /*Just a Divide by Zero Exception*/
                            }

                            try {
                                await Task.Delay((int)(waitTime * 1000), token);
                            } catch (TaskCanceledException) { break; }

                            // HeartBeat ON
                            foreach (var handler in _gameHandlers)
                            {
                                handler.UpdateHeartBeat(true, false);
                            }

                            try {
                                await Task.Delay(100, token);
                            } catch (TaskCanceledException) { break; }
                        }
                        else
                        {
                            _logger.LogWarning("Cannot beat as HR is Less than or equal to zero");
                            try {
                                await Task.Delay(1000, token);
                            } catch (TaskCanceledException) { break; }
                        }
                    }
                    else
                    {
                        foreach (var handler in _gameHandlers)
                        {
                            handler.UpdateHeartBeat(false, true);
                        }

                        _logger.LogDebug("Waiting for ActiveHRManager for beating");
                        waited = true;
                        try {
                            await Task.Delay(1000, token);
                        } catch (TaskCanceledException) { break; }
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot beat as ActiveHRManager is null!");
                    try {
                        await Task.Delay(1000, token);
                    } catch (TaskCanceledException) { break; }
                }
            }
        }
    }

    private currentHRSplit intToHRSplit(int hr)
    {
        var chs = new currentHRSplit();
        if (hr < 0)
            _logger.LogError("HeartRate is below zero.");
        else
        {
            var currentNumber = hr.ToString().Select(x => int.Parse(x.ToString()));
            var numbers = currentNumber.ToArray();
            if (hr <= 9)
            {
                // why is your HR less than 10????
                try
                {
                    chs.ones = numbers[0];
                    chs.tens = 0;
                    chs.hundreds = 0;
                }
                catch (Exception) { }
            }
            else if (hr <= 99)
            {
                try
                {
                    chs.ones = numbers[1];
                    chs.tens = numbers[0];
                    chs.hundreds = 0;
                }
                catch (Exception) { }
            }
            else if (hr >= 100)
            {
                try
                {
                    chs.ones = numbers[2];
                    chs.tens = numbers[1];
                    chs.hundreds = numbers[0];
                }
                catch (Exception) { }
            }
            // if your heart rate is above 999 then you need to see a doctor
            // for real what
        }

        return chs;
    }

    private class currentHRSplit
    {
        public int hundreds;
        public int ones;
        public int tens;
    }

}
