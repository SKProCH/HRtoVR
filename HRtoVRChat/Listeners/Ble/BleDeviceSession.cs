using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Ble;

public sealed class BleDeviceSession : ReactiveObject, IAsyncDisposable {
    private readonly IDevice _device;

    private readonly CompositeDisposable _disposables = new();
    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly ILogger _logger;
    private readonly BehaviorSubject<Guid?> _targetCharacteristicId = new(null);
    private readonly BehaviorSubject<Guid?> _targetServiceId = new(null);

    public BleDeviceSession(IDevice device, ILogger logger) {
        _device = device;
        _logger = logger;

        // Discovered services pipeline
        Observable.FromAsync(_ => DiscoverServicesInternalAsync(CancellationToken.None))
            .Subscribe()
            .DisposeWith(_disposables);

        // Discovered characteristics pipeline
        _targetServiceId
            .DistinctUntilChanged()
            .Select(serviceId => {
                DiscoveredCharacteristics = [];
                return serviceId == null
                    ? Observable.Empty<Unit>()
                    : Observable.FromAsync(ct => DiscoverCharacteristicsInternalAsync(serviceId.Value, ct))
                        .Select(_ => Unit.Default);
            })
            .Switch()
            .Subscribe()
            .DisposeWith(_disposables);

        // Heart rate subscription pipeline
        _targetServiceId
            .CombineLatest(_targetCharacteristicId, (s, c) => (ServiceId: s, CharacteristicId: c))
            .DistinctUntilChanged()
            .Select(x => {
                if (x.ServiceId == null || x.CharacteristicId == null)
                    return Observable.Return(0);

                return Observable.Create<int>(async (observer, ct) => {
                    try {
                        var service = await _device.GetServiceAsync(x.ServiceId.Value, ct);
                        if (service == null) {
                            _logger.LogWarning("Service {ServiceId} not found", x.ServiceId);
                            return Disposable.Empty;
                        }

                        var characteristic = await service.GetCharacteristicAsync(x.CharacteristicId.Value, ct);
                        if (characteristic == null) {
                            _logger.LogWarning("Characteristic {CharacteristicId} not found", x.CharacteristicId);
                            return Disposable.Empty;
                        }

                        void OnValueUpdated(object? sender, CharacteristicUpdatedEventArgs e) {
                            var hr = ParseHeartRate(e.Characteristic.Value);
                            observer.OnNext(hr);
                        }

                        characteristic.ValueUpdated += OnValueUpdated;
                        await characteristic.StartUpdatesAsync(ct);

                        // ReSharper disable once AsyncVoidMethod
                        return Disposable.Create(async void () => {
                            characteristic.ValueUpdated -= OnValueUpdated;
                            try {
                                await characteristic.StopUpdatesAsync();
                            }
                            catch (Exception ex) {
                                _logger.LogWarning(ex, "Error stopping updates for characteristic {CharId}",
                                    characteristic.Id);
                            }
                        });
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) {
                        _logger.LogError(ex, "Error in heart rate subscription for {ServiceId}/{CharacteristicId}",
                            x.ServiceId, x.CharacteristicId);
                        return Disposable.Empty;
                    }
                });
            })
            .Switch()
            .Subscribe(hr => _heartRate.OnNext(hr))
            .DisposeWith(_disposables);
    }

    public IObservable<int> HeartRate => _heartRate;
    public IObservable<Guid?> TargetServiceId => _targetServiceId;
    public IObservable<Guid?> TargetCharacteristicId => _targetCharacteristicId;
    public Guid DeviceId => _device.Id;

    [Reactive] public IReadOnlyList<BleDescriptor> DiscoveredServices { get; private set; } = [];
    [Reactive] public IReadOnlyList<BleCharacteristic> DiscoveredCharacteristics { get; private set; } = [];

    public async ValueTask DisposeAsync() {
        try
        {
            await CrossBluetoothLE.Current.Adapter.DisconnectDeviceAsync(_device, CancellationToken.None);
        }
        catch (Exception) {
            // ignored
        }

        Dispose();
    }

    public void UpdateConfiguration(Guid? serviceId, Guid? characteristicId) {
        _logger.LogDebug("UpdateConfiguration: service={ServiceId}, characteristic={CharId}", serviceId, characteristicId);
        _targetServiceId.OnNext(serviceId);
        _targetCharacteristicId.OnNext(characteristicId);
    }

    private async Task DiscoverServicesInternalAsync(CancellationToken ct) {
        try {
            _logger.LogInformation("Discovering services for device {DeviceId}...", _device.Id);
            var services =
                await BleExtensions.RetryWithDelayAsync(token => _device.GetServicesAsync(token),
                    cancellationToken: ct);
            ct.ThrowIfCancellationRequested();
            DiscoveredServices = services.Select(s => new BleDescriptor(s.Id, s.Name ?? "Unknown Service")).ToArray();
            _logger.LogInformation("Discovered {Count} services for device {DeviceId}: [{Services}]",
                DiscoveredServices.Count, _device.Id,
                string.Join(", ", DiscoveredServices.Select(s => $"{s.Name} ({s.Id})")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error discovering services for device {DeviceId}", _device.Id);
            DiscoveredServices = [];
        }
    }

    private async Task DiscoverCharacteristicsInternalAsync(Guid serviceId, CancellationToken ct) {
        try {
            _logger.LogInformation("Discovering characteristics for service {ServiceId}...", serviceId);
            var service = await _device.GetServiceAsync(serviceId, ct);
            if (service == null) {
                _logger.LogWarning("Service {ServiceId} not found during characteristic discovery", serviceId);
                return;
            }

            var characteristics =
                await BleExtensions.RetryWithDelayAsync(token => service.GetCharacteristicsAsync(token),
                    cancellationToken: ct);
            ct.ThrowIfCancellationRequested();
            DiscoveredCharacteristics = characteristics
                .Select(c => new BleCharacteristic(c.Id, c.Name ?? "Unknown Characteristic", c.CanUpdate))
                .ToArray();
            _logger.LogInformation("Discovered {Count} characteristics for service {ServiceId}: [{Characteristics}]",
                DiscoveredCharacteristics.Count, serviceId,
                string.Join(", ", DiscoveredCharacteristics.Select(c => $"{c.Name} ({c.Id}, canUpdate={c.CanUpdate})")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error discovering characteristics for service {ServiceId}", serviceId);
            DiscoveredCharacteristics = [];
        }
    }

    private int ParseHeartRate(byte[] data) {
        if (data.Length < 2) return 0;

        var flags = data[0];
        var isUint16 = (flags & 0x01) != 0;

        if (isUint16) {
            if (data.Length < 3) return 0;
            return BitConverter.ToUInt16(data, 1);
        }

        return data[1];
    }

    public void Dispose() {
        _disposables.Dispose();
    }
}