using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure;
using Plugin.BLE;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.Listeners.Ble;

public class BleHrListener : StartStopServiceBase, IHrListener {
    private readonly ILogger<BleHrListener> _logger;
    private readonly IOptionsMonitor<BleOptions> _options;

    private readonly BehaviorSubject<int> _heartRate = new(0);
    private readonly BehaviorSubject<bool> _isConnected = new(false);

    public string Name => "Bluetooth LE";
    public IObservable<int> HeartRate => _heartRate;
    public IObservable<bool> IsConnected => _isConnected;
    public Type SettingsViewModelType => typeof(ViewModels.Listeners.BleSettingsViewModel);
    [Reactive] public IReadOnlyList<BleDescriptor>? Services { get; private set; }
    [Reactive] public IReadOnlyList<BleCharacteristic>? Characteristics { get; private set; }

    public BleHrListener(ILogger<BleHrListener> logger, IOptionsMonitor<BleOptions> options) {
        _logger = logger;
        _options = options;
    }

    protected override async Task Run(CompositeDisposable disposables, CancellationToken token) {
        var nestedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var options = _options.CurrentValue;

        _options.OnChange(HandleOptionsChanged)
            .DisposeNullableWith(disposables);

        await RunIt();
        return;

        void HandleOptionsChanged(BleOptions newOptions) {
            if (options.Device != newOptions.Device) {
                // If we changed the device, lets fully restart
                _ = RunIt();
            }
        }

        async Task RunIt() {
            var oldCts = nestedCts;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            nestedCts = cts;
            await oldCts.CancelAsync();
            if (_options.CurrentValue.Device is null) {
                _logger.LogWarning("No valid BLE device selected in options");
                return;
            }

            await RunCore(cts.Token);
        }
    }

    private async Task RunCore(CancellationToken token) {
        var adapter = CrossBluetoothLE.Current.Adapter;
        var attempt = 1;
        while (!token.IsCancellationRequested) {
            var deviceId = _options.CurrentValue.Device!.Id;
            try {
                Services = null;
                Characteristics = null;

                _logger.LogInformation("Connecting to BLE device: {DeviceId}", deviceId);
                var device = await adapter.ConnectToKnownDeviceAsync(deviceId,
                    cancellationToken: token);

                if (device == null) {
                    var millisecondsDelay = Math.Min(2000 * attempt++, 30_000);
                    _logger.LogError("Device {DeviceId} not found, waiting {Delay} ms", deviceId, millisecondsDelay);
                    await Task.Delay(millisecondsDelay, token);
                    continue;
                }

                attempt = 1;

                try {
                    _logger.LogInformation("Device {DeviceId} connected. Discovering services", deviceId);
                    Debug.Assert(device is not null);

                    while (!token.IsCancellationRequested && device!.State == DeviceState.Connected) {
                        var serviceId = _options.CurrentValue.Service?.Id;
                        IService? service = null;
                        if (serviceId is not null) {
                            service = await device.GetServiceAsync(serviceId.Value, token);
                        }

                        if (service is null) {
                            _logger.LogInformation("Discovering all services");
                            var services = await device.GetServicesAsync(token);
                            var bleDescriptors = services.Select(s => new BleDescriptor(s.Id, s.Name ?? "Unknown Service"));
                            Services = bleDescriptors.ToArray();

                            var recovered = await WaitForRecoveryAsync(device, opts => opts.Service?.Id == serviceId);
                            if (!recovered) break;

                            // Service id changed, lets try again
                            continue;
                        }

                        _logger.LogInformation("Discovering characteristics for service {ServiceUuid}...", service.Id);
                        var characteristicId = _options.CurrentValue.Characteristic?.Id;
                        ICharacteristic? characteristic = null;
                        if (characteristicId is not null) {
                            characteristic = await service.GetCharacteristicAsync(characteristicId.Value, token);
                        }

                        if (characteristic is null) {
                            _logger.LogInformation("Discovering all characteristics");

                            var characteristics = await service.GetCharacteristicsAsync(token);
                            var bleDescriptors = characteristics.Select(c =>
                                new BleCharacteristic(c.Id, c.Name ?? "Unknown Characteristic", c.CanUpdate));
                            Characteristics = bleDescriptors.ToArray();
                            var recovered = await WaitForRecoveryAsync(device, opts =>
                                opts.Characteristic?.Id == characteristicId &&
                                opts.Service?.Id == serviceId);

                            if (!recovered) break;
                            // Service or characteristic id changed, lets try again
                            continue;
                        }


                        _logger.LogInformation("Subscribing to notifications for {CharUuid}...", characteristic.Id);

                        characteristic.ValueUpdated += OnCharacteristicValueUpdated;

                        try {
                            await characteristic.StartUpdatesAsync(token);

                            _isConnected.OnNext(true);
                            _logger.LogInformation("BLE Heart Rate listener active");

                            // Keep connection alive until cancelled, disconnected, or target device/service/char changes
                            while (!token.IsCancellationRequested &&
                                   device?.State == DeviceState.Connected &&
                                   _options.CurrentValue.Device.Id == deviceId &&
                                   _options.CurrentValue.Service?.Id == service.Id &&
                                   _options.CurrentValue.Characteristic?.Id == characteristic.Id) {
                                await Task.Delay(500, token);
                            }
                        }
                        finally {
                            _isConnected.OnNext(false);
                            await characteristic.StopUpdatesAsync(CancellationToken.None);
                        }
                    }
                }
                finally {
                    await adapter.DisconnectDeviceAsync(device, CancellationToken.None);
                }

            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(ex, "Error in BLE listener for {DeviceId}", deviceId);
                _isConnected.OnNext(false);
                await Task.Delay(2000, token);
            }
        }

        async Task<bool> WaitForRecoveryAsync(IDevice device, Func<BleOptions, bool> isConfigStillBad) {
            var tcs = new TaskCompletionSource<bool>();

            // 1. Если отменили токен всего приложения
            using var tokenReg = token.Register(() => tcs.TrySetCanceled());

            // 2. Слушаем изменение конфига
            using var configSub = _options.OnChange(newOpts => {
                // Если конфиг изменился так, что старая ошибка больше не актуальна
                if (!isConfigStillBad(newOpts)) {
                    tcs.TrySetResult(true); // true = конфиг исправлен, можно пробовать снова
                }
            });

            // 3. Слушаем разрыв соединения (вдруг девайс сам отвалится, пока мы ждем конфиг)
            EventHandler<DeviceEventArgs> disconnectHandler = (s, e) => {
                if (e.Device.Id == device?.Id) {
                    tcs.TrySetResult(false); // false = устройство отключилось
                }
            };
            adapter.DeviceDisconnected += disconnectHandler;

            try {
                // Проверка перед началом ожидания (вдруг конфиг уже поменяли или девайс уже отпал)
                if (!isConfigStillBad(_options.CurrentValue)) return true;
                if (device?.State != DeviceState.Connected) return false;

                _logger.LogWarning("Waiting for configuration change while keeping connection...");

                // ЖДЕМ ЗДЕСЬ
                return await tcs.Task;
            }
            finally {
                adapter.DeviceDisconnected -= disconnectHandler;
            }
        }
    }

    private void OnCharacteristicValueUpdated(object? s, CharacteristicUpdatedEventArgs e) {
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
}