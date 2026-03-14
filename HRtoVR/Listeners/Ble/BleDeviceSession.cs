using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Models;
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

    // New state to control lazy discovery
    private readonly BehaviorSubject<bool> _discoveryMode = new(false);

    // Connection state
    private readonly BehaviorSubject<ConnectionState> _state = new(ConnectionState.Connecting);

    public BleDeviceSession(IDevice device, ILogger logger) {
        _device = device;
        _logger = logger;

        // Service discovery pipeline (triggers once after DiscoveryMode is enabled)
        _discoveryMode
            .Where(isEnabled => isEnabled)
            .Take(1) // Discover services once per session
            .Select(_ => Observable.FromAsync(DiscoverServicesInternalAsync))
            .Switch()
            .Subscribe()
            .DisposeWith(_disposables);

        // Characteristic discovery pipeline (triggers on service change ONLY if DiscoveryMode is enabled)
        Observable.CombineLatest(
                _targetServiceId.DistinctUntilChanged(),
                _discoveryMode.DistinctUntilChanged(),
                (serviceId, isDiscovery) => (ServiceId: serviceId, IsDiscovery: isDiscovery)
            )
            .Select(x => {
                DiscoveredCharacteristics = []; // Clear old characteristics
                if (!x.IsDiscovery || x.ServiceId == null)
                    return Observable.Empty<Unit>();

                return Observable.FromAsync(ct => DiscoverCharacteristicsInternalAsync(x.ServiceId.Value, ct))
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
                        // Attempt to get the specific service (fast operation without full scan)
                        var service = await _device.GetServiceAsync(x.ServiceId.Value, ct);
                        if (service == null) {
                            _logger.LogWarning("Service {ServiceId} not found. Triggering fallback discovery",
                                x.ServiceId);
                            EnableDiscovery(); // If service not found (invalid settings) -> trigger full scan
                            return Disposable.Empty;
                        }

                        // Attempt to get the specific characteristic
                        var characteristic = await service.GetCharacteristicAsync(x.CharacteristicId.Value, ct);
                        if (characteristic == null) {
                            _logger.LogWarning(
                                "Characteristic {CharacteristicId} not found. Triggering fallback discovery",
                                x.CharacteristicId);
                            EnableDiscovery(); // If characteristic not found -> trigger full scan
                            return Disposable.Empty;
                        }

                        void OnValueUpdated(object? sender, CharacteristicUpdatedEventArgs e) {
                            var hr = ParseHeartRate(e.Characteristic.Value);
                            observer.OnNext(hr);
                        }

                        characteristic.ValueUpdated += OnValueUpdated;
                        await characteristic.StartUpdatesAsync(ct);

                        _state.OnNext(ConnectionState.Active);

                        // ReSharper disable once AsyncVoidMethod
                        return Disposable.Create(async void () => {
                            _state.OnNext(ConnectionState.Connecting);
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
    public IObservable<ConnectionState> State => _state;

    [Reactive] public IReadOnlyList<BleDescriptor> DiscoveredServices { get; private set; } = [];
    [Reactive] public IReadOnlyList<BleCharacteristic> DiscoveredCharacteristics { get; private set; } = [];

    public void EnableDiscovery() {
        if (!_discoveryMode.Value) {
            _logger.LogInformation("Enabling discovery mode for device {DeviceId}", _device.Id);
            _discoveryMode.OnNext(true);
        }
    }

    public ValueTask DisposeAsync() {
        _ = Task.Run(async () => {
            try {
                await CrossBluetoothLE.Current.Adapter.DisconnectDeviceAsync(_device, CancellationToken.None);
            }
            catch (Exception) {
                // ignored
            }
        });
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void UpdateConfiguration(Guid? serviceId, Guid? characteristicId) {
        _logger.LogDebug("UpdateConfiguration: service={ServiceId}, characteristic={CharId}", serviceId,
            characteristicId);
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
            DiscoveredServices = services
                .DistinctBy(service => service.Id)
                .Select(s => new BleDescriptor(s.Id, s.Name ?? "Unknown Service"))
                .ToArray();
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
                .DistinctBy(characteristic => characteristic.Id)
                .Select(c => new BleCharacteristic(c.Id, c.Name ?? "Unknown Characteristic", c.CanUpdate))
                .ToArray();
            _logger.LogInformation("Discovered {Count} characteristics for service {ServiceId}: [{Characteristics}]",
                DiscoveredCharacteristics.Count, serviceId,
                string.Join(", ",
                    DiscoveredCharacteristics.Select(c => $"{c.Name} ({c.Id}, canUpdate={c.CanUpdate})")));
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