using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using HRtoVR.Configs;
using HRtoVR.Infrastructure;
using LucHeart.CoreOSC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OscQueryLibrary;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.GameHandlers;

public class VrChatOscHandler(
    IOptionsMonitor<AppOptions> appOptions,
    IOptionsMonitor<VrChatOscOptions> vrcOptions,
    ILogger<VrChatOscHandler> logger)
    : StartStopServiceBase, IGameHandler {
    private OscQueryServer? _oscQueryServer;
    private OscDuplex? _oscDuplex;
    private CancellationTokenSource? _connectionCts;

    private readonly Dictionary<string, object> _lastSentValues = new();

    public string Name => "VRChatOSC";
    [Reactive] public bool IsConnected { get; private set; }

    protected override async Task Run(CompositeDisposable disposables, CancellationToken token) {
        _oscQueryServer = new OscQueryServer("HRtoVR", IPAddress.Loopback);
        await _oscQueryServer.FoundVrcClient.SubscribeAsync(OnFoundVrcClient);
        _oscQueryServer.Start();

        token.Register(() => {
            _oscQueryServer?.Dispose();
            _oscQueryServer = null;
        });

        logger.LogInformation("OSCQuery server started, waiting for VRChat...");

        await Task.Delay(Timeout.Infinite, token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task OnFoundVrcClient(IPEndPoint endPoint) {
        logger.LogInformation("VRChat OSCQuery client found at {EndPoint}", endPoint);

        _connectionCts?.Cancel();
        _connectionCts?.Dispose();
        _oscDuplex?.Dispose();
        _oscDuplex = null;

        RxApp.MainThreadScheduler.Schedule(Unit.Default, (_, _) => { IsConnected = false; return Disposable.Empty; });

        _connectionCts = new CancellationTokenSource();
        var localEndPoint = new IPEndPoint(endPoint.Address, _oscQueryServer!.OscReceivePort);
        _oscDuplex = new OscDuplex(localEndPoint, endPoint);

        RxApp.MainThreadScheduler.Schedule(Unit.Default, (_, _) => { IsConnected = true; return Disposable.Empty; });

        logger.LogInformation("OSC connection established. Sending to {Remote}, receiving on port {LocalPort}",
            endPoint, _oscQueryServer.OscReceivePort);

        ResendAllParameters();

        _ = Task.Run(() => ReceiverLoop(_connectionCts.Token), _connectionCts.Token);
    }

    private async Task ReceiverLoop(CancellationToken token) {
        try {
            while (!token.IsCancellationRequested) {
                var message = await _oscDuplex!.ReceiveMessageAsync();
                if (message.Address == "/avatar/change") {
                    logger.LogInformation("Avatar change detected, resending parameters...");
                    _oscQueryServer?.RefreshParameters();
                    ResendAllParameters();
                }
            }
        }
        catch (OperationCanceledException) {
            // Expected on disconnect/stop
        }
        catch (ObjectDisposedException) {
            // OscDuplex was disposed during reconnect
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "VRChat disconnected or OSC receiver error");
            RxApp.MainThreadScheduler.Schedule(Unit.Default, (_, _) => { IsConnected = false; return Disposable.Empty; });
        }

        if (!token.IsCancellationRequested && !IsConnected) {
            logger.LogInformation("VRChat connection lost, waiting for reconnect...");
        }
    }

    private void ResendAllParameters() {
        foreach (var kv in _lastSentValues) {
            SendMessageDirect(kv.Key, kv.Value);
        }
    }

    public override Task Stop() {
        if (IsConnected) {
            Update(0, 0f, false);
        }

        _connectionCts?.Cancel();
        _connectionCts?.Dispose();
        _connectionCts = null;

        _oscDuplex?.Dispose();
        _oscDuplex = null;

        _oscQueryServer?.Dispose();
        _oscQueryServer = null;

        _lastSentValues.Clear();
        IsConnected = false;

        return base.Stop();
    }

    public void Update(int heartBeat, float heartBeatPercentage, bool isConnected) {
        var parameterNames = appOptions.CurrentValue.ParameterNames;

        SendIfChanged(parameterNames.HundredsHR, heartBeat / 100 % 10);
        SendIfChanged(parameterNames.TensHR, heartBeat / 10 % 10);
        SendIfChanged(parameterNames.OnesHR, heartBeat % 10);
        SendIfChanged(parameterNames.HR, Math.Clamp(heartBeat, 0, 255));
        SendIfChanged(parameterNames.HRPercent, heartBeatPercentage);
        SendIfChanged(parameterNames.FullHRPercent, 2f * heartBeatPercentage - 1f);
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
            SendMessageDirect(address, value);
            _lastSentValues[address] = value;
        }
    }

    private void SendMessageDirect(string address, object value) {
        if (value is bool b && vrcOptions.CurrentValue.UseLegacyBool) {
            value = b ? 1 : 0;
        }

        var message = new OscMessage("/avatar/parameters/" + address, value);
        _oscDuplex?.SendAsync(message);
    }
}
