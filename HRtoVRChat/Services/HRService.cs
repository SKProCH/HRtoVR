using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using HRtoVRChat.GameHandlers;
using HRtoVRChat.HRManagers;
using HRtoVRChat_OSC_SDK;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Services;

public class HRService : IHRService
{
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IOSCService _oscService;
    private readonly IParamsService _paramsService;
    private readonly IOSCAvatarListener _oscAvatarListener;

    private HRType hrType = HRType.Unknown;
    private HRManager? activeHRManager;
    private readonly AppBridge _appBridge = new();
    private bool isRestarting;
    private bool RunHeartBeat;

    private List<IGameHandler> _gameHandlers = new();

    private CustomTimer? loopCheck;

    private Thread? VerifyVRCOpen;
    private CancellationTokenSource vvoToken = new();
    private Thread? BeatThread;
    private CancellationTokenSource btToken = new();

    private readonly string HelpCommandString = "\n\n[Help]\n" +
                                                       "exit - Exits the app.\n" +
                                                       "starthr - Manually starts the HeartRateManager if it isn't already started.\n" +
                                                       "stophr - Manually stops the HeartRateManager if it is already started.\n" +
                                                       "restarthr - Stops then Starts the HeartRateManager.\n" +
                                                       "startbeat - Starts HeartBeat if it isn't enabled already.\n" +
                                                       "stopbeat - Stops the HeartBeat if it is already started.\n" +
                                                       "refreshconfig - Refreshes the Config from File.\n" +
                                                       "biassdk [sdkname] - Forces a specific SDK to be used (SDK hrType only)\n" +
                                                       "unbiassdk - Does not prefer any SDK (SDK hrType only)\n" +
                                                       "destroysdk [sdkname] - Unloads an SDK by name\n" +
                                                       "help - Shows available commands.\n";

    public CustomTimer? BoopUwUTimer;

    public static string[] Gargs { get; private set; } = { };

    private bool _lastHeartBeatState;

    public HRService(IOptionsMonitor<AppOptions> appOptions, IOSCService oscService, IParamsService paramsService, IOSCAvatarListener oscAvatarListener)
    {
        _appOptions = appOptions;
        _oscService = oscService;
        _paramsService = paramsService;
        _oscAvatarListener = oscAvatarListener;
    }

    public void Start(string[] args)
    {
        Gargs = args;
        // _configService.CreateConfig(); // Handled by DI/App.axaml.cs
        _oscService.Init();

        // Initialize Game Handlers
        _gameHandlers.Clear();
        var vrcHandler = new VRChatOSCHandler(_paramsService, _appOptions);
        _gameHandlers.Add(vrcHandler);

        if (Enumerable.Contains(args, "--neos-bridge"))
        {
            LogHelper.Log("Enabling NeosBridge Handler");
            var neosHandler = new NeosHandler(_appOptions);
            _gameHandlers.Add(neosHandler);
            NeosHandler.OnCommand += command => HandleCommand(command, true);
        }

        bool foundOnStart = _gameHandlers.Any(gh => gh.IsGameRunning());

        _oscAvatarListener.Init();
        _appBridge.InitServer(() =>
        {
            if (activeHRManager != null)
            {
                try
                {
                    Messages.AppBridgeMessage apm = new()
                    {
                        CurrentSourceName = activeHRManager.GetName(),
                        CurrentAvatar = _oscAvatarListener.CurrentAvatar?.ToAvatarInfo()
                    };

                    var hr = activeHRManager.GetHR();
                    var split = intToHRSplit(hr);

                    apm.HR = hr;
                    apm.onesHR = split.ones;
                    apm.tensHR = split.tens;
                    apm.hundredsHR = split.hundreds;
                    apm.isHRConnected = activeHRManager.IsOpen();
                    apm.isHRActive = activeHRManager.IsActive();
                    apm.isHRBeat = _lastHeartBeatState;

                    // Calculate Percentages
                    var maxhr = (float)_appOptions.CurrentValue.MaxHR;
                    var minhr = (float)_appOptions.CurrentValue.MinHR;
                    float targetFloat = 0;
                    if (hr > maxhr) targetFloat = 1;
                    else if (hr < minhr) targetFloat = 0;
                    else targetFloat = (hr - minhr) / (maxhr - minhr);

                    apm.HRPercent = targetFloat;
                    apm.FullHRPercent = 2f * targetFloat - 1f;

                    return apm;
                }
                catch (Exception e)
                {
                    LogHelper.Error("Failed to forward data from activeHRManager to AppBridge!", e);
                    return null;
                }
            }

            return null;
        });
        if (!Directory.Exists(SDKManager.SDKsLocation))
            Directory.CreateDirectory(SDKManager.SDKsLocation);

        if (foundOnStart)
        {
            Check();
        }
        else
        {
            if (Enumerable.Contains(args, "--auto-start"))
            {
                LogHelper.Log("No supported game found! Waiting...");
                loopCheck = new CustomTimer(5000, ct => LoopCheck());
            }
            else
            {
                Check();
            }
        }
    }

    private void LoopCheck()
    {
        var foundGame = _gameHandlers.Any(gh => gh.IsGameRunning());
        if (foundGame)
        {
            LogHelper.Log("Found Game! Starting...");
            Check();
        }
    }

    private void Check()
    {
        var fromAutoStart = Enumerable.Contains(Gargs, "--auto-start");
        bool gameRunning = _gameHandlers.Any(gh => gh.IsGameRunning());

        if (gameRunning || Enumerable.Contains(Gargs, "--skip-vrc-check"))
        {
            if (loopCheck?.IsRunning ?? false)
                loopCheck.Close();
            StartInternal();
        }
        else
        {
            if (fromAutoStart)
            {
                loopCheck.Close();
                loopCheck = new CustomTimer(5000, ct => LoopCheck());
            }

            // Save all logs to file
            var dt = DateTime.Now;
            LogHelper.SaveToFile($"{dt.Hour}-{dt.Minute}-{dt.Second}-{dt.Millisecond} {dt.Day}-{dt.Month}-{dt.Year}");
            // Exit
            LogHelper.Warn("No supported game was detected!");
        }
    }

    public void HandleCommand(string? input)
    {
        HandleCommand(input, false);
    }

    private void HandleCommand(string? input, bool fromBridge = false)
    {
        var inputs = input?.Split(' ') ?? new string[0];
        if (inputs.Length == 0) return;

        switch (inputs[0].ToLower())
        {
            case "help":
                LogHelper.Log(HelpCommandString);
                break;
            case "exit":
                Stop(true);
                break;
            case "starthr":
                StartHRListener();
                break;
            case "stophr":
                StopHRListener();
                break;
            case "restarthr":
                RestartHRListener();
                break;
            case "startbeat":
                if (BeatThread?.IsAlive ?? false)
                    LogHelper.Warn("Cannot start beat as it's already started!");
                else
                {
                    RunHeartBeat = true;
                    btToken = new CancellationTokenSource();
                    BeatThread = new Thread(() =>
                    {
                        RunHeartBeat = true;
                        HeartBeat();
                    });
                    BeatThread.Start();
                    LogHelper.Log("Started HeartBeat");
                }

                break;
            case "stopbeat":
                if (BeatThread?.IsAlive ?? false)
                {
                    try
                    {
                        btToken.Cancel();
                    }
                    catch (Exception e) { LogHelper.Debug(e); }

                    RunHeartBeat = false;
                }

                LogHelper.Log("Stopped HRBeat");
                break;
            case "refreshconfig":
                _paramsService.ResetParams();
                // _configService.CreateConfig(); // Config reloads automatically with IOptionsMonitor
                // We might need to re-init params if VRChat handler is active
                foreach (var handler in _gameHandlers)
                {
                    if (handler is VRChatOSCHandler vrcHandler)
                    {
                        _paramsService.InitParams();
                    }
                }
                break;
            case "biassdk":
                if (inputs.Length > 1 && !string.IsNullOrEmpty(inputs[1]))
                    SDKManager.PreferredSDK = inputs[1];
                break;
            case "unbiassdk":
                SDKManager.PreferredSDK = string.Empty;
                break;
            case "destroysdk":
                if (activeHRManager != null && inputs.Length > 1 && !string.IsNullOrEmpty(inputs[1]))
                {
                    if (activeHRManager is SDKManager s)
                    {
                        s.DestroySDKByName(inputs[1]);
                    }
                }

                break;
            default:
                LogHelper.Warn($"Unknown Command \"{inputs[0]}\"!");
                break;
        }
    }

    private void StartInternal()
    {
        if (!Enumerable.Contains(Gargs, "--skip-vrc-check"))
        {
            vvoToken = new CancellationTokenSource();
            VerifyVRCOpen = new Thread(() =>
            {
                var isOpen = _gameHandlers.Any(gh => gh.IsGameRunning());
                while (!vvoToken.IsCancellationRequested)
                {
                    isOpen = _gameHandlers.Any(gh => gh.IsGameRunning());
                    if (!isOpen)
                        vvoToken.Cancel();
                    Thread.Sleep(1500);
                }

                LogHelper.Log("Thread Stopped");
                var fromAutoStart = Enumerable.Contains(Gargs, "--auto-start");
                Stop(!fromAutoStart, fromAutoStart);
            });
            VerifyVRCOpen.Start();
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
                LogHelper.Error($"Failed to start handler {handler.Name}", e);
            }
        }

        // Continue
        StartHRListener();
        // Start Coroutine
        BoopUwUTimer = new CustomTimer(1000, ct => BoopUwU());
        LogHelper.Log("Started");
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
                LogHelper.Error("Failed to stop ActiveHRManager", e);
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
                LogHelper.Error($"Failed to stop handler {handler.Name}", e);
            }
        }

        // Stop Extraneous Tasks
        if (loopCheck?.IsRunning ?? false)
            loopCheck.Close();

        LogHelper.Log("Stopped");
        // Quit the App
        if (quitApp)
        {
            // Save all logs to file
            var dt = DateTime.Now;
            LogHelper.SaveToFile($"{dt.Hour}-{dt.Minute}-{dt.Second}-{dt.Millisecond} {dt.Day}-{dt.Month}-{dt.Year}");
            // Stop AppBridge
            _appBridge.StopServer();
            // Exit
            // Environment.Exit(0);
        }

        if (autoStart)
        {
            LogHelper.Log("Restarting when Game Detected");
            loopCheck = new CustomTimer(5000, ct => LoopCheck());
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
            Task.Factory.StartNew(() =>
            {
                while (loops <= 2)
                {
                    Task.Delay(1000);
                    loops++;
                }

                isRestarting = false;
                StartHRListener(true);
            });
        }
    }

    private HRType StringToHRType(string input)
    {
        var hrt = HRType.Unknown;
        switch (input.ToLower())
        {
            case "fitbithrtows":
                hrt = HRType.FitbitHRtoWS;
                break;
            case "hrproxy":
                hrt = HRType.HRProxy;
                break;
            case "hyperate":
                hrt = HRType.HypeRate;
                break;
            case "pulsoid":
                hrt = HRType.Pulsoid;
                break;
            case "stromno":
                hrt = HRType.Stromno;
                break;
            case "pulsoidsocket":
                hrt = HRType.PulsoidSocket;
                break;
            case "textfile":
                hrt = HRType.TextFile;
                break;
            case "omnicept":
                hrt = HRType.Omnicept;
                break;
            case "sdk":
                hrt = HRType.SDK;
                break;
        }

        return hrt;
    }

    public void StartHRListener(bool fromRestart = false)
    {
        // Start Manager based on Config
        hrType = StringToHRType(_appOptions.CurrentValue.HrType);
        // Check activeHRManager
        if (activeHRManager != null)
        {
            if (activeHRManager.IsActive())
            {
                LogHelper.Warn("HRListener is currently active! Stop it first");
                return;
            }
        }

        switch (hrType)
        {
            case HRType.FitbitHRtoWS:
                activeHRManager = new FitbitManager();
                activeHRManager.Init(_appOptions.CurrentValue.FitbitOptions.Url);
                break;
            case HRType.HRProxy:
                activeHRManager = new HRProxyManager();
                activeHRManager.Init(_appOptions.CurrentValue.HRProxyOptions.Id);
                break;
            case HRType.HypeRate:
                activeHRManager = new HypeRateManager();
                activeHRManager.Init(_appOptions.CurrentValue.HypeRateOptions.SessionId);
                break;
            case HRType.Pulsoid:
                LogHelper.Warn(
                    "\n=========================================================================================\n" +
                    "WARNING ABOUT PULSOID\n" +
                    "It is detected that you're using the Pulsoid Method for grabbing HR Data,\n" +
                    "Please note that this method will soon be DEPRECATED and replaced with PulsoidSocket!\n" +
                    "Please see the URL below on how to upgrade!\n" +
                    "https://github.com/200Tigersbloxed/HRtoVRChat_OSC/wiki/Upgrading-from-Pulsoid-to-PulsoidSocket \n" +
                    "=========================================================================================\n\n" +
                    "Starting Pulsoid in 25 Seconds...");
                Thread.Sleep(25000);
                activeHRManager = new PulsoidManager();
                activeHRManager.Init(_appOptions.CurrentValue.PulsoidOptions.Widget);
                break;
            case HRType.Stromno:
                activeHRManager = new PulsoidManager();
                activeHRManager.Init(_appOptions.CurrentValue.StromnoOptions.Widget);
                break;
            case HRType.PulsoidSocket:
                activeHRManager = new PulsoidSocketManager();
                activeHRManager.Init(_appOptions.CurrentValue.PulsoidSocketOptions.Key);
                break;
            case HRType.TextFile:
                activeHRManager = new TextFileManager();
                activeHRManager.Init(_appOptions.CurrentValue.TextFileOptions.Location);
                break;
            // TODO Omnicept Temporarily Disabled
            // case HRType.Omnicept:
            //     activeHRManager = new OmniceptManager();
            //     activeHRManager.Init(String.Empty);
            //     break;
            case HRType.SDK:
                activeHRManager = new SDKManager();
                activeHRManager.Init("127.0.0.1:9000");
                break;
            default:
                LogHelper.Warn("No hrType was selected! Please see README if you think this is an error!");
                Stop(true);
                break;
        }

        // Start HeartBeats if there was a valid choice
        if (!RunHeartBeat)
        {
            if (BeatThread?.IsAlive ?? false)
            {
                try
                {
                    btToken.Cancel();
                }
                catch (Exception) { }

                RunHeartBeat = false;
            }

            btToken = new CancellationTokenSource();
            BeatThread = new Thread(() =>
            {
                LogHelper.Log("Starting Beating!");
                RunHeartBeat = true;
                HeartBeat();
            });
            BeatThread.Start();
        }
        else if (activeHRManager == null)
            LogHelper.Warn("Can't start beat as ActiveHRManager is null!");
    }

    public void StopHRListener()
    {
        if (activeHRManager != null)
        {
            if (!activeHRManager.IsActive())
            {
                LogHelper.Warn("HRListener is currently inactive! Attempting to stop anyways.");
                //return;
            }

            activeHRManager.Stop();
        }

        activeHRManager = null;
        // Stop Beating
        if (BeatThread?.IsAlive ?? false)
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

    private void HeartBeat()
    {
        var waited = false;
        while (!btToken.IsCancellationRequested)
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
                                LogHelper.Log("Found ActiveHRManager! Starting HeartBeat.");
                                waited = false;
                            }

                            // HeartBeat OFF
                            _lastHeartBeatState = false;
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

                            Thread.Sleep((int)(waitTime * 1000));

                            // HeartBeat ON
                            _lastHeartBeatState = true;
                            foreach (var handler in _gameHandlers)
                            {
                                handler.UpdateHeartBeat(true, false);
                            }

                            Thread.Sleep(100);
                        }
                        else
                        {
                            LogHelper.Warn("Cannot beat as HR is Less than or equal to zero");
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        foreach (var handler in _gameHandlers)
                        {
                            handler.UpdateHeartBeat(false, true);
                        }

                        LogHelper.Debug("Waiting for ActiveHRManager for beating");
                        waited = true;
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    LogHelper.Warn("Cannot beat as ActiveHRManager is null!");
                    Thread.Sleep(1000);
                }
            }
        }
    }

    private currentHRSplit intToHRSplit(int hr)
    {
        var chs = new currentHRSplit();
        if (hr < 0)
            LogHelper.Error("HeartRate is below zero.");
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

    private enum HRType
    {
        FitbitHRtoWS,
        HRProxy,
        HypeRate,
        Pulsoid,
        Stromno,
        PulsoidSocket,
        TextFile,
        Omnicept,
        SDK,
        Unknown
    }
}
