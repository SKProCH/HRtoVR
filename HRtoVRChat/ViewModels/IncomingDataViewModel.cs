using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HRtoVRChat_OSC_SDK;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class IncomingDataViewModel : ViewModelBase
{
    [Reactive] public string AppBridgeStatus { get; set; } = "Not Connected";
    [Reactive] public string IncomingDataOutput { get; set; } = "";

    private CancellationTokenSource? _cancellationTokenSource;
    private AppBridge? _appBridge;

    public IncomingDataViewModel()
    {
        Initialize();
    }

    private void Initialize()
    {
        StartBackgroundThread();
    }

    private void StartBackgroundThread()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => {
            var attemptConnect = false;
            while (!_cancellationTokenSource.IsCancellationRequested) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    AppBridgeStatus = "App Bridge Connection Status: " +
                        (_appBridge?.IsClientConnected ?? false ? "Connected" : "Not Connected");
                });

                if (SoftwareManager.IsSoftwareRunning && !attemptConnect && !(_appBridge?.IsClientConnected ?? false)) {
                    _appBridge = new AppBridge();
                    _appBridge.OnAppBridgeMessage += async message => {
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            var avatarParameters = string.Empty;
                            foreach (var currentAvatarParameter in message.CurrentAvatar?.parameters ?? new List<string>())
                                avatarParameters += currentAvatarParameter + "\n";

                            IncomingDataOutput = $"Current Source: {message.CurrentSourceName}\n\n" +
                                                      "-- Parameters --\n" +
                                                      $"onesHR: {message.onesHR}\n" +
                                                      $"tensHR: {message.tensHR}\n" +
                                                      $"hundredsHR: {message.hundredsHR}\n" +
                                                      $"isHRConnected: {message.isHRConnected}\n" +
                                                      $"isHRActive: {message.isHRActive}\n" +
                                                      $"isHRBeat: {message.isHRBeat} (inaccurate over AppBridge)\n" +
                                                      $"HRPercent: {message.HRPercent}\n" +
                                                      $"FullHRPercent: {message.FullHRPercent}\n" +
                                                      $"HR: {message.HR}\n\n" +
                                                      "-- Current Avatar --\n" +
                                                      $"name: {message.CurrentAvatar?.name ?? "unknown"}\n" +
                                                      $"id: {message.CurrentAvatar?.id ?? "unknown"}\n" +
                                                      "== parameters ==\n" +
                                                      $"{avatarParameters}";
                        });
                    };
                    _appBridge.OnClientDisconnect += async () => {
                        _appBridge.StopClient();
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            IncomingDataOutput = "";
                        });
                    };
                    _appBridge.InitClient();
                    attemptConnect = true;
                }
                else
                    attemptConnect = false;

                Thread.Sleep(10);
            }
        }, _cancellationTokenSource.Token);
    }
}
