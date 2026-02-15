using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Ble;

public sealed class BleNotificationClient : ReactiveObject, IAsyncDisposable {
    private readonly IService _service;
    private readonly ILogger _logger;
    private readonly Guid? _characteristicId;
    private readonly Subject<int> _heartRate = new();

    public IObservable<int> HeartRate => _heartRate;

    [Reactive] public IReadOnlyList<BleCharacteristic> Characteristics { get; private set; } = Array.Empty<BleCharacteristic>();
    [Reactive] public ICharacteristic? Characteristic { get; private set; }

    public BleNotificationClient(IService service, Guid? characteristicId, ILogger logger) {
        _service = service;
        _characteristicId = characteristicId;
        _logger = logger;
    }

    public async Task<ICharacteristic?> GetCharacteristicAsync(CancellationToken ct) {
        if (_characteristicId == null) return null;
        try {
            Characteristic = await _service.GetCharacteristicAsync(_characteristicId.Value, ct);
            return Characteristic;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error getting characteristic {CharId} for service {ServiceId}", _characteristicId, _service.Id);
            Characteristic = null;
            return null;
        }
    }

    public async Task<IReadOnlyList<BleCharacteristic>> DiscoverCharacteristicsAsync(CancellationToken ct) {
        _logger.LogInformation("Discovering all characteristics for service {ServiceId}", _service.Id);
        try {
            var characteristics = await _service.GetCharacteristicsAsync(ct);
            Characteristics = characteristics.Select(c =>
                new BleCharacteristic(c.Id, c.Name ?? "Unknown Characteristic", c.CanUpdate)).ToArray();
            return Characteristics;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error discovering characteristics for service {ServiceId}", _service.Id);
            Characteristics = Array.Empty<BleCharacteristic>();
            return Characteristics;
        }
    }

    public async Task StartUpdatesAsync(CancellationToken ct) {
        if (Characteristic == null) throw new InvalidOperationException("No characteristic selected");

        _logger.LogInformation("Subscribing to notifications for {CharUuid}...", Characteristic.Id);
        Characteristic.ValueUpdated += OnValueUpdated;
        await Characteristic.StartUpdatesAsync(ct);
    }

    public async Task StopUpdatesAsync() {
        if (Characteristic != null) {
            _logger.LogInformation("Unsubscribing from notifications for {CharUuid}...", Characteristic.Id);
            Characteristic.ValueUpdated -= OnValueUpdated;
            try {
                await Characteristic.StopUpdatesAsync();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error stopping updates for characteristic {CharId}", Characteristic.Id);
            }
        }
    }

    private void OnValueUpdated(object? sender, CharacteristicUpdatedEventArgs e) {
        var hr = ParseHeartRate(e.Characteristic.Value);
        _heartRate.OnNext(hr);
    }

    private int ParseHeartRate(byte[] data) {
        if (data == null || data.Length < 2) return 0;

        var flags = data[0];
        var isUint16 = (flags & 0x01) != 0;

        if (isUint16) {
            if (data.Length < 3) return 0;
            return BitConverter.ToUInt16(data, 1);
        }

        return data[1];
    }

    public async ValueTask DisposeAsync() {
        await StopUpdatesAsync();
    }
}
