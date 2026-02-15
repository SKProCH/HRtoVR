using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure;
using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Ble;

public sealed class BleDeviceWrapper : ReactiveObject, IAsyncDisposable {
    private readonly IAdapter _adapter;
    private readonly ILogger _logger;
    private readonly Guid _deviceId;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    [Reactive] public Guid? ServiceId { get; private set; }
    [Reactive] public Guid? CharacteristicId { get; private set; }

    [Reactive] public IDevice? Device { get; private set; }
    [Reactive] public BleServiceWrapper? ServiceWrapper { get; private set; }
    [ObservableAsProperty] public bool IsConnected { get; }

    public IObservable<int> HeartRate => this.WhenAnyValue(x => x.ServiceWrapper)
        .Select(wrapper => wrapper?.HeartRate ?? Observable.Empty<int>())
        .Switch();

    public BleDeviceWrapper(Guid deviceId, Guid? serviceId, Guid? characteristicId, IAdapter adapter, ILogger logger) {
        _deviceId = deviceId;
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
        _adapter = adapter;
        _logger = logger;

        var connected = Observable.FromEventPattern<EventHandler<DeviceEventArgs>, DeviceEventArgs>(
                h => _adapter.DeviceConnected += h,
                h => _adapter.DeviceConnected -= h)
            .Where(e => e.EventArgs.Device.Id == _deviceId)
            .Select(_ => true);

        var disconnected = Observable.FromEventPattern<EventHandler<DeviceEventArgs>, DeviceEventArgs>(
                h => _adapter.DeviceDisconnected += h,
                h => _adapter.DeviceDisconnected -= h)
            .Where(e => e.EventArgs.Device.Id == _deviceId)
            .Select(_ => false);

        var deviceChanged = this.WhenAnyValue(x => x.Device)
            .Select(d => d?.State == DeviceState.Connected);

        connected.Merge(disconnected).Merge(deviceChanged)
            .DistinctUntilChanged()
            .ToPropertyEx(this, x => x.IsConnected);
    }
    
    public void UpdateConfiguration(Guid? serviceId, Guid? characteristicId) {
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
    }

    public async Task ManagedRunAsync(CancellationToken ct) {
        // Reactive subscription to configuration changes
        using var configSub = this.WhenAnyValue(x => x.ServiceId, x => x.CharacteristicId)
            .Skip(1) // Skip initial values
            .Where(_ => IsConnected)
            .SelectMany(_ => Observable.FromAsync(() => SyncConfigurationAsync(ct)))
            .Subscribe();

        var attempt = 1;
        while (!ct.IsCancellationRequested) {
            try {
                if (!IsConnected) {
                    var device = await ConnectAsync(ct);
                    if (device == null) {
                        await WaitRetry(ct, attempt++);
                        continue;
                    }
                    attempt = 1; // Reset attempt on successful connection
                }

                _logger.LogInformation("Device {DeviceId} connected. Syncing configuration", _deviceId);
                await SyncConfigurationAsync(ct);

                // Wait for either a disconnection or a cancellation
                var disconnected = this.WhenAnyValue(x => x.IsConnected)
                    .Where(connected => !connected)
                    .FirstAsync()
                    .ToTask(ct);

                await Task.WhenAny(disconnected, ct.WaitAsync());
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(ex, "Error in BLE managed loop for {DeviceId}", _deviceId);
                await WaitRetry(ct, attempt++);
            }
            finally {
                if (!IsConnected) {
                    // If we are disconnected, cleanup sync state so we re-sync on reconnect
                    await CleanupSyncStateAsync();
                }
            }
        }
    }

    private async Task CleanupSyncStateAsync() {
        await _syncLock.WaitAsync();
        try {
            if (ServiceWrapper != null) {
                await ServiceWrapper.DisposeAsync();
                ServiceWrapper = null;
            }
        }
        finally {
            _syncLock.Release();
        }
    }

    public async Task<bool> SyncConfigurationAsync(CancellationToken ct) {
        await _syncLock.WaitAsync(ct);
        try {
            if (Device == null) return false;

            // Sync Service
            if (ServiceWrapper?.Service?.Id != ServiceId) {
                if (ServiceWrapper != null) {
                    await ServiceWrapper.DisposeAsync();
                }

                ServiceWrapper = new BleServiceWrapper(Device, ServiceId, _logger);
                var service = await ServiceWrapper.GetServiceAsync(ct);

                if (service == null) {
                    _logger.LogWarning("Service {ServiceId} not found for device {DeviceId}", ServiceId, _deviceId);
                    await ServiceWrapper.DiscoverServicesAsync(ct);
                    return false;
                }
            }

            // Sync Characteristic
            if (ServiceWrapper != null) {
                if (!await ServiceWrapper.SyncCharacteristicAsync(CharacteristicId, ct)) {
                    return false;
                }
            }
            else {
                return false;
            }

            _logger.LogInformation("BLE Connection chain synchronized for {DeviceId}", _deviceId);
            return true;
        }
        finally {
            _syncLock.Release();
        }
    }

    private async Task WaitRetry(CancellationToken ct, int attempt) {
        var delay = Math.Min(2000 * attempt, 30_000);
        _logger.LogError("BLE operation failed, waiting {Delay} ms before retry", delay);
        await Task.Delay(delay, ct);
    }

    public async Task<IDevice?> ConnectAsync(CancellationToken ct) {
        _logger.LogInformation("Connecting to BLE device: {DeviceId}", _deviceId);
        try {
            Device = await _adapter.ConnectToKnownDeviceAsync(_deviceId, cancellationToken: ct);
            return Device;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Failed to connect to BLE device {DeviceId}", _deviceId);
            Device = null;
            return null;
        }
    }

    public async Task DisconnectAsync() {
        if (ServiceWrapper != null) {
            await ServiceWrapper.DisposeAsync();
            ServiceWrapper = null;
        }

        if (Device != null) {
            _logger.LogInformation("Disconnecting from BLE device: {DeviceId}", Device.Id);
            try {
                await _adapter.DisconnectDeviceAsync(Device);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error during disconnection from device {DeviceId}", Device.Id);
            }
            finally {
                Device = null;
            }
        }
    }

    public async ValueTask DisposeAsync() {
        await DisconnectAsync();
    }
}
