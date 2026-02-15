using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure;
using Plugin.BLE;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plugin.BLE.Abstractions.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using HRtoVRChat.Infrastructure.Options;

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
    [Reactive] public BleDeviceWrapper? DeviceWrapper { get; private set; }

    public BleHrListener(ILogger<BleHrListener> logger, IOptionsMonitor<BleOptions> options) {
        _logger = logger;
        _options = options;
    }

    protected override async Task Run(CompositeDisposable disposables, CancellationToken token) {
        var nestedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _options.Observe()
            .Select(options => options.Device)
            .Prepend(_options.CurrentValue.Device)
            .DistinctUntilChanged()
            .Subscribe(newOptions => {
                nestedCts.Cancel();
                nestedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                _ = RunCore(nestedCts.Token);
            })
            .DisposeWith(disposables);
        
        _options.Observe()
            .Select(options => options.Service)
            .DistinctUntilChanged()
            .Subscribe(newService => {
                DeviceWrapper?.UpdateConfiguration(newService?.Id, DeviceWrapper.CharacteristicId);
            });
        
        _options.Observe()
            .Select(options => options.Characteristic)
            .DistinctUntilChanged()
            .Subscribe(newCharacteristic => {
                DeviceWrapper?.UpdateConfiguration(DeviceWrapper.ServiceId, newCharacteristic?.Id);
            });
        
        this.WhenAnyValue(x => x.DeviceWrapper)
            .Select(dw => dw?.WhenAnyValue(x => x.IsConnected) ?? Observable.Return(false))
            .Switch()
            .DistinctUntilChanged()
            .Subscribe(connected => _isConnected.OnNext(connected))
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.DeviceWrapper)
            .Select(dw => dw?.HeartRate ?? Observable.Return(0))
            .Switch()
            .Subscribe(hr => _heartRate.OnNext(hr))
            .DisposeWith(disposables);
    }

    private async Task RunCore(CancellationToken token) {
        var currentOptions = _options.CurrentValue;
        if (currentOptions.Device is null) {
            _logger.LogWarning("No valid BLE device selected in options");
            return;
        }

        var deviceId = currentOptions.Device.Id;
        var deviceWrapper = new BleDeviceWrapper(deviceId, currentOptions.Service?.Id,
            currentOptions.Characteristic?.Id,
            CrossBluetoothLE.Current.Adapter, _logger);
        DeviceWrapper = deviceWrapper;

        try {
            await deviceWrapper.ManagedRunAsync(token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(ex, "Error in BLE listener for {DeviceId}", deviceId);
            await Task.Delay(2000, token);
        }
        finally {
            await DeviceWrapper.DisposeAsync();
            if (DeviceWrapper == deviceWrapper)
                DeviceWrapper = null;
        }
    }
}