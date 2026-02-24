using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure;
using Plugin.BLE;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using HRtoVRChat.Infrastructure.Options;
using Plugin.BLE.Abstractions.Exceptions;

namespace HRtoVRChat.Listeners.Ble;

public class BleHrListener(ILogger<BleHrListener> logger, IOptionsMonitor<BleOptions> options)
    : StartStopServiceBase, IHrListener {
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);

    public string Name => "Bluetooth LE";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
    public Type SettingsViewModelType => typeof(ViewModels.Listeners.BleSettingsViewModel);

    [Reactive] public BleDeviceSession? Session { get; private set; }

    protected override async Task Run(CompositeDisposable disposables, CancellationToken token) {
        // Track session state for IsConnected
        this.WhenAnyValue(x => x.Session)
            .Select(s => s != null)
            .Subscribe(connected => _isConnected.OnNext(connected))
            .DisposeWith(disposables);

        // Proxy heart rate from session
        this.WhenAnyValue(x => x.Session)
            .Select(s => s?.HeartRate ?? Observable.Return(0))
            .Switch()
            .Subscribe(hr => _heartRate.OnNext(hr))
            .DisposeWith(disposables);

        // Sync configuration to session when options change
        options.Observe()
            .Subscribe(o => {
                if (Session?.DeviceId == o.Device?.Id) {
                    Session?.UpdateConfiguration(o.Service?.Id, o.Characteristic?.Id);
                }
            })
            .DisposeWith(disposables);

        // Manage connection lifecycle based on configured device
        options.Observe()
            .Select(o => o.Device?.Id)
            .DistinctUntilChanged()
            .Do(_ => Session = null)
            .Select(deviceId => deviceId == null
                ? Observable.Empty<Unit>()
                : Observable.FromAsync(ct => Task.Run(() => ConnectionLoop(deviceId.Value, ct), ct)))
            .Switch()
            .Subscribe()
            .DisposeWith(disposables);

        await Task.Delay(-1, token);
    }

    private async Task ConnectionLoop(Guid deviceId, CancellationToken token) {
        var attempt = 1;
        while (!token.IsCancellationRequested) {
            BleDeviceSession? session = null;
            try {
                logger.LogInformation("Connecting to BLE device {DeviceId} (attempt #{Attempt})", deviceId, attempt);
                var device = await CrossBluetoothLE.Current.Adapter.ConnectToKnownDeviceAsync(
                        deviceId, default, token.WithTimeout(TimeSpan.FromSeconds(10)))
                    .WithTimeout(TimeSpan.FromSeconds(10));

                logger.LogInformation("Connected to BLE device {DeviceId} (name={DeviceName}, state={State})",
                    device.Id, device.Name, device.State);
                attempt = 1;

                await using (session = new BleDeviceSession(device, logger)) {
                    Session = session;

                    // Sync initial configuration
                    var currentOptions = options.CurrentValue;
                    session.UpdateConfiguration(currentOptions.Service?.Id, currentOptions.Characteristic?.Id);

                    // Wait until device disconnects or connection task is cancelled
                    var tcs = new TaskCompletionSource();
                    await using var _ = token.Register(() => tcs.TrySetResult());

                    // Watchdog: force disconnect if no HR data for 30s while characteristic is configured
                    using var watchdog = session.TargetCharacteristicId
                        .Select(charId => charId == null
                            ? Observable.Never<int>()
                            : session.HeartRate.Where(hr => hr > 0).Timeout(TimeSpan.FromSeconds(30)))
                        .Switch()
                        .Subscribe(
                            _ => { },
                            ex => {
                                if (ex is TimeoutException) {
                                    logger.LogWarning(
                                        "No heart rate data for 30 seconds from device {DeviceId}, forcing disconnect",
                                        device.Id);
                                    tcs.TrySetResult();
                                }
                            });

                    void OnDisconnected(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e) {
                        if (e.Device.Id == device.Id) {
                            logger.LogInformation("BLE Device {DeviceId} disconnected", e.Device.Id);
                            tcs.TrySetResult();
                        }
                    }

                    CrossBluetoothLE.Current.Adapter.DeviceDisconnected += OnDisconnected;
                    try {
                        await tcs.Task;
                    }
                    finally {
                        CrossBluetoothLE.Current.Adapter.DeviceDisconnected -= OnDisconnected;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
                return;
            }
            catch (TimeoutException) {
                attempt++;
                logger.LogWarning("Timeout connecting to BLE device {DeviceId}, retrying", deviceId);
            }
            catch (Exception ex) {
                var delay = Math.Min(2000 * attempt++, 15000);
                logger.LogError(ex is DeviceConnectionException ? null : ex,
                    "Error in BLE connection loop for device {DeviceId}, retrying in {DelayMs}ms", deviceId, delay);
                await Task.Delay(delay, token);
            }
            finally {
                if (Session == session)
                    Session = null;
            }
        }
    }
}