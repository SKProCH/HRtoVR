using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Listeners.Ble;

public class BleHrListener : StartStopServiceBase, IHrListener
{
    private readonly ILogger<BleHrListener> _logger;
    private readonly IOptionsMonitor<BleOptions> _options;

    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);

    public string Name => "Bluetooth LE";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
    public Type SettingsViewModelType => typeof(ViewModels.Listeners.BleSettingsViewModel);

    public BleHrListener(ILogger<BleHrListener> logger, IOptionsMonitor<BleOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    protected override async Task Run(CancellationToken token)
    {
        var adapter = CrossBluetoothLE.Current.Adapter;

        while (!token.IsCancellationRequested)
        {
            var deviceIdStr = _options.CurrentValue.SelectedDeviceId;
            if (string.IsNullOrEmpty(deviceIdStr) || !Guid.TryParse(deviceIdStr, out var deviceId))
            {
                _logger.LogWarning("No valid BLE device selected in options.");
                await Task.Delay(5000, token);
                continue;
            }

            try
            {
                _logger.LogInformation("Connecting to BLE device: {DeviceId}", deviceId);

                // Plugin.BLE doesn't have FromIdAsync, we need to connect by ID or find it in discovered devices
                // ConnectToKnownDeviceAsync is usually the way for saved IDs
                var device = await adapter.ConnectToKnownDeviceAsync(deviceId, cancellationToken: token);

                if (device == null)
                {
                    _logger.LogError("Device {DeviceId} not found", deviceId);
                    await Task.Delay(5000, token);
                    continue;
                }

                _logger.LogInformation("Connected. Discovering services...");
                var targetServiceGuid = Guid.Parse(_options.CurrentValue.ServiceGuid);
                var service = await device.GetServiceAsync(targetServiceGuid, token);

                if (service == null)
                {
                    _logger.LogError("Service {ServiceGuid} not found on device", targetServiceGuid);
                    await adapter.DisconnectDeviceAsync(device);
                    await Task.Delay(5000, token);
                    continue;
                }

                _logger.LogInformation("Discovering characteristics for service {ServiceUuid}...", service.Id);
                var targetCharGuid = Guid.Parse(_options.CurrentValue.CharacteristicGuid);
                var characteristic = await service.GetCharacteristicAsync(targetCharGuid);

                if (characteristic == null)
                {
                    _logger.LogError("Characteristic {CharGuid} not found", targetCharGuid);
                    await adapter.DisconnectDeviceAsync(device);
                    await Task.Delay(5000, token);
                    continue;
                }

                _logger.LogInformation("Subscribing to notifications for {CharUuid}...", characteristic.Id);

                characteristic.ValueUpdated += (s, e) =>
                {
                    var hr = ParseHeartRate(e.Characteristic.Value);
                    if (hr > 0)
                    {
                        _heartRate.OnNext(hr);
                    }
                };

                await characteristic.StartUpdatesAsync();

                _isConnected.OnNext(true);
                _logger.LogInformation("BLE Heart Rate listener active.");

                // Keep connection alive until cancelled or disconnected
                while (!token.IsCancellationRequested && device.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                {
                    await Task.Delay(1000, token);
                }

                _isConnected.OnNext(false);
                await characteristic.StopUpdatesAsync();
                await adapter.DisconnectDeviceAsync(device);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in BLE listener for {DeviceId}", deviceId);
                _isConnected.OnNext(false);
                await Task.Delay(5000, token); // Retry after delay
            }
        }
    }

    private int ParseHeartRate(byte[] data)
    {
        if (data == null || data.Length < 2) return 0;

        byte flags = data[0];
        bool isUint16 = (flags & 0x01) != 0;

        if (isUint16)
        {
            if (data.Length < 3) return 0;
            return BitConverter.ToUInt16(data, 1);
        }

        return data[1];
    }
}
