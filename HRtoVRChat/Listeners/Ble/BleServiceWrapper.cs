using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Ble;

public sealed class BleServiceWrapper : ReactiveObject, IAsyncDisposable {
    private readonly IDevice _device;
    private readonly ILogger _logger;
    private readonly Guid? _serviceId;

    [Reactive] public IReadOnlyList<BleDescriptor> Services { get; private set; } = Array.Empty<BleDescriptor>();
    [Reactive] public IService? Service { get; private set; }
    [Reactive] public BleNotificationClient? NotificationClient { get; private set; }

    public IObservable<int> HeartRate => this.WhenAnyValue(x => x.NotificationClient)
        .Select(client => client?.HeartRate ?? Observable.Empty<int>())
        .Switch();

    public BleServiceWrapper(IDevice device, Guid? serviceId, ILogger logger) {
        _device = device;
        _serviceId = serviceId;
        _logger = logger;
    }

    public async Task<IService?> GetServiceAsync(CancellationToken ct) {
        if (_serviceId == null) return null;
        try {
            Service = await _device.GetServiceAsync(_serviceId.Value, ct);
            return Service;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error getting service {ServiceId} for device {DeviceId}", _serviceId, _device.Id);
            Service = null;
            return null;
        }
    }

    public async Task<bool> SyncCharacteristicAsync(Guid? characteristicId, CancellationToken ct) {
        if (Service == null) return false;

        if (NotificationClient?.Characteristic?.Id != characteristicId) {
            if (NotificationClient != null) {
                await NotificationClient.DisposeAsync();
                NotificationClient = null;
            }

            NotificationClient = new BleNotificationClient(Service, characteristicId, _logger);
            var characteristic = await NotificationClient.GetCharacteristicAsync(ct);

            if (characteristic == null) {
                _logger.LogWarning("Characteristic {CharacteristicId} not found for service {ServiceId}",
                    characteristicId, _serviceId);
                await NotificationClient.DiscoverCharacteristicsAsync(ct);
                return false;
            }

            await NotificationClient.StartUpdatesAsync(ct);
        }

        return true;
    }

    public async Task<IReadOnlyList<BleDescriptor>> DiscoverServicesAsync(CancellationToken ct) {
        _logger.LogInformation("Discovering all services for device {DeviceId}", _device.Id);
        try {
            var services = await _device.GetServicesAsync(ct);
            Services = services.Select(s => new BleDescriptor(s.Id, s.Name ?? "Unknown Service")).ToArray();
            return Services;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error discovering services for device {DeviceId}", _device.Id);
            Services = Array.Empty<BleDescriptor>();
            return Services;
        }
    }

    public async ValueTask DisposeAsync() {
        if (NotificationClient != null) {
            await NotificationClient.DisposeAsync();
            NotificationClient = null;
        }
    }
}
