using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Configs;
using HRtoVRChat.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Vizcon.OSC;

namespace HRtoVRChat.GameHandlers;

public class VrChatOscHandler : StartStopServiceBase, IGameHandler {
    private readonly IOptionsMonitor<AppOptions> _appOptions;
    private readonly IOptionsMonitor<VrChatOscOptions> _vrcOptions;
    private readonly ILogger _logger;

    private UDPListener? _listener;
    private UDPSender? _sender;

    // Last sent values to avoid redundant OSC messages
    private readonly Dictionary<string, object> _lastSentValues = new();

    public string Name => "VRChatOSC";
    [Reactive] public bool IsConnected { get; private set; }

    public VrChatOscHandler(IOptionsMonitor<AppOptions> appOptions, IOptionsMonitor<VrChatOscOptions> vrcOptions,
        ILogger<VrChatOscHandler> logger) {
        _appOptions = appOptions;
        _vrcOptions = vrcOptions;
        _logger = logger;
    }

    protected override async Task Run(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            // TODO: Handle remote VRChat process
            var vrchatProcess = GetVrchatProcess();

            if (vrchatProcess != null) {
                IsConnected = true;
                InitializeOsc();
                await vrchatProcess.WaitForExitAsync(token);
            }

            IsConnected = false;

            try {
                await Task.Delay(2000, token);
            }
            catch (OperationCanceledException) {
                break;
            }
        }
    }

    public override void Stop() {
        if (IsConnected) {
            // Reset parameters to default values on stop
            Update(0, 0f, false);
        }
        
        // Reset last sent values tracking
        _lastSentValues.Clear();

        try {
            _listener?.Close();
            _sender = null;
        }
        catch {
            // ignored
        }

        IsConnected = false;
    }

    private Process? GetVrchatProcess() {
        return Process.GetProcessesByName("VRChat").FirstOrDefault()
               ?? (_vrcOptions.CurrentValue.ExpandCVR
                   ? Process.GetProcessesByName("ChilloutVR").FirstOrDefault()
                   : null);
    }

    private void InitializeOsc() {
        _logger.LogInformation("Initializing OSC Senders and Listeners");
        _sender = new UDPSender(_vrcOptions.CurrentValue.Ip, _vrcOptions.CurrentValue.Port);
        try {
            _listener = new UDPListener(_vrcOptions.CurrentValue.ReceiverPort,
                packet => OnOscMessage((OscMessage?)packet));
        }
        catch (Exception e) {
            _logger.LogWarning(e, "Failed to initialize OSC Listener on port {Port}. It might already be in use",
                _vrcOptions.CurrentValue.ReceiverPort);
        }
    }

    private void SendMessage(string address, object value) {
        // If it's a bool, it needs to be converted to a 0, 1 format
        if (value is bool b && _vrcOptions.CurrentValue.UseLegacyBool) {
            value = b ? 1 : 0;
        }

        var message = new OscMessage("/avatar/parameters/" + address, value);
        _sender?.Send(message);
    }

    private void OnOscMessage(OscMessage? message) {
        if (message?.Address == "/avatar/change") {
            _logger.LogInformation("Avatar change detected! Resending parameters...");
            foreach (var lastSentValue in _lastSentValues) {
                SendMessage(lastSentValue.Key, lastSentValue.Value);
            }
        }
    }

    public void Update(int heartBeat, float heartBeatPercentage, bool isConnected) {
        var parameterNames = _appOptions.CurrentValue.ParameterNames;

        // Discrete HR digits
        SendIfChanged(parameterNames.HundredsHR, heartBeat / 100 % 10);
        SendIfChanged(parameterNames.TensHR, heartBeat / 10 % 10);
        SendIfChanged(parameterNames.OnesHR, heartBeat % 10);

        // Raw HR (clamped to 255 as per original implementation)
        SendIfChanged(parameterNames.HR, Math.Clamp(heartBeat, 0, 255));

        // HR Percentages
        SendIfChanged(parameterNames.HRPercent, heartBeatPercentage);
        SendIfChanged(parameterNames.FullHRPercent, 2f * heartBeatPercentage - 1f);

        // Connection/Active status
        SendIfChanged(parameterNames.IsHRConnected, isConnected);
        SendIfChanged(parameterNames.IsHRActive, isConnected && heartBeat > 0);
    }

    private void SendIfChanged(string address, object value) {
        var changed = !_lastSentValues.TryGetValue(address, out var lastValue);

        if (!changed) {
            if (value is float f1 && lastValue is float f2) {
                changed = Math.Abs(f1 - f2) > 0.001f;
            }
            else {
                changed = !Equals(value, lastValue);
            }
        }

        if (changed) {
            SendMessage(address, value);
            _lastSentValues[address] = value;
        }
    }
}