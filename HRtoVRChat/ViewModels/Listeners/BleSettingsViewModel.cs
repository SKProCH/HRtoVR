using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using HRtoVRChat.Infrastructure;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.Listeners.Ble;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels.Listeners;

public class BleSettingsViewModel : ViewModelBase, IListenerSettingsViewModel, IActivatableViewModel {
    private readonly ILogger<BleSettingsViewModel> _logger;
    private readonly IAdapter _adapter;
    private readonly ReadOnlyObservableCollection<BleDescriptor> _displayedDevices;

    public ViewModelActivator Activator { get; } = new();

    public ReadOnlyObservableCollection<BleDescriptor> DisplayedDevices => _displayedDevices;

    public SourceCache<BleDescriptor, Guid> DiscoveredDevices { get; } = new(device => device.Id);
    [Reactive] public BleDescriptor? ActiveDevice { get; set; }
    [Reactive] public BleDescriptor? SelectedDevice { get; set; }

    [Reactive] public IReadOnlyList<BleDescriptor> Services { get; set; } = [];
    [Reactive] public BleDescriptor? ActiveService { get; set; }

    [Reactive] public IReadOnlyList<BleDescriptor> Characteristics { get; set; } = [];
    [Reactive] public BleDescriptor? ActiveCharacteristic { get; set; }

    public BleSettingsViewModel(BleHrListener bleListener, IOptionsManager<BleOptions> optionsManager,
        ILogger<BleSettingsViewModel> logger) {
        _logger = logger;
        _adapter = CrossBluetoothLE.Current.Adapter;

        ActiveDevice = optionsManager.CurrentValue.Device;
        if (optionsManager.CurrentValue.Service is { } service) {
            Services = [service];
            ActiveService = service;
        }

        if (optionsManager.CurrentValue.Characteristic is { } characteristic) {
            Characteristics = [characteristic];
            ActiveCharacteristic = characteristic;
        }

        DiscoveredDevices.Connect()
            .AutoRefreshOnObservable(_ => this
                .WhenAnyValue(x => x.ActiveDevice)
                .Select(_ => Unit.Default))
            .Filter(device => device.Id != optionsManager.CurrentValue.Device?.Id)
            .Bind(out _displayedDevices)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedDevice)
            .WhereNotNull()
            .Subscribe(ChangeDevice);

        this.WhenAnyValue(x => x.ActiveDevice)
            .Skip(1)
            .Subscribe(descriptor => {
                ActiveService = null;
                optionsManager.CurrentValue.Device = descriptor;
            });

        this.WhenAnyValue(x => x.ActiveService)
            .Skip(1)
            .Subscribe(descriptor => {
                ActiveCharacteristic = null;
                optionsManager.CurrentValue.Service = descriptor;
            });

        this.WhenAnyValue(x => x.ActiveCharacteristic)
            .Subscribe(descriptor => optionsManager.CurrentValue.Characteristic = descriptor);

        // From listener
        bleListener.WhenAnyValue(x => x.DeviceWrapper)
            .Select(dw => dw?.WhenAnyValue(x => x.ServiceWrapper) ?? Observable.Return<BleServiceWrapper?>(null))
            .Switch()
            .Select(sw => sw?.WhenAnyValue(x => x.Services) ?? Observable.Return<IReadOnlyList<BleDescriptor>>([]))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnServicesDiscovered);

        bleListener.WhenAnyValue(x => x.DeviceWrapper)
            .Select(dw => dw?.WhenAnyValue(x => x.ServiceWrapper) ?? Observable.Return<BleServiceWrapper?>(null))
            .Switch()
            .Select(sw => sw?.WhenAnyValue(x => x.NotificationClient) ?? Observable.Return<BleNotificationClient?>(null))
            .Switch()
            .Select(nc => nc?.WhenAnyValue(x => x.Characteristics) ?? Observable.Return<IReadOnlyList<BleCharacteristic>>([]))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnCharacteristicsDiscovered);

        this.WhenActivated(disposables => {
            _ = StartScanAsync(disposables.RegisterToken());
        });
    }

    private async Task StartScanAsync(CancellationToken token) {
        DiscoveredDevices.Clear();

        try {
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.ScanTimeout = int.MaxValue;
            await _adapter.StartScanningForDevicesAsync(cancellationToken: token);
        }
        catch (OperationCanceledException) {
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error scanning for devices");
        }
        finally {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        }
    }

    private static string[] CommonDeviceNames => ["HR", "Heart", "Polar", "Garmin", "HW"];

    private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e) {
        var deviceDescriptor = new BleDescriptor(e.Device.Id, e.Device.Name ?? "Unknown Device");
        DiscoveredDevices.AddOrUpdate(deviceDescriptor);

        // Trying to guess
        if (ActiveDevice is null && e.Device.Name is not null) {
            if (CommonDeviceNames.Any(s => e.Device.Name.Contains(s, StringComparison.OrdinalIgnoreCase))) {
                ChangeDevice(deviceDescriptor);
            }
        }
    }

    private static readonly Guid HeartRateServiceUuid = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");

    private void OnServicesDiscovered(IReadOnlyList<BleDescriptor>? obj) {
        Services = obj ?? [];
        if (obj is null)
            return;

        ActiveService ??= obj.FirstOrDefault(x => x.Id == HeartRateServiceUuid)
                          ?? obj.FirstOrDefault(x => x.Name.Contains("Heart", StringComparison.OrdinalIgnoreCase));
    }

    private static readonly Guid HeartRateMeasurementCharacteristicUuid =
        Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb");

    private void OnCharacteristicsDiscovered(IReadOnlyList<BleCharacteristic>? obj) {
        Characteristics = obj ?? [];
        if (obj is null)
            return;

        ActiveCharacteristic ??= obj.FirstOrDefault(x => x.Id == HeartRateMeasurementCharacteristicUuid)
                                 ?? obj.FirstOrDefault(x => x.CanUpdate);
    }

    /// <summary>
    /// Device changed by the user
    /// </summary>
    private void ChangeDevice(BleDescriptor descriptor) {
        SelectedDevice = null;
        ActiveDevice = descriptor;
        ActiveService = null;
    }
}