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
    private readonly IOptionsManager<BleOptions> _optionsManager;
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
        _optionsManager = optionsManager;
        _logger = logger;
        _adapter = CrossBluetoothLE.Current.Adapter;

        ActiveDevice = _optionsManager.CurrentValue.Device;
        ActiveService = _optionsManager.CurrentValue.Service;
        ActiveCharacteristic = _optionsManager.CurrentValue.Characteristic;

        DiscoveredDevices.Connect()
            .AutoRefreshOnObservable(_ => this
                .WhenAnyValue(x => x.ActiveDevice)
                .Select(_ => Unit.Default))
            .Filter(device => device.Id != _optionsManager.CurrentValue.Device?.Id)
            .Bind(out _displayedDevices)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedDevice)
            .WhereNotNull()
            .Subscribe(ChangeDevice);

        this.WhenAnyValue(x => x.ActiveDevice)
            .Subscribe(descriptor => {
                ActiveService = null;
                _optionsManager.CurrentValue.Device = descriptor;
                // _optionsManager.Save();
            });

        this.WhenAnyValue(x => x.ActiveService)
            .Subscribe(descriptor => {
                ActiveCharacteristic = null;
                _optionsManager.CurrentValue.Service = descriptor;
            });

        this.WhenAnyValue(x => x.ActiveCharacteristic)
            .Subscribe(descriptor => _optionsManager.CurrentValue.Characteristic = descriptor);

        // From listener
        bleListener.WhenAnyValue(x => x.Services)
            .BindTo(this, x => x.Services);

        bleListener.WhenAnyValue(x => x.Characteristics)
            .BindTo(this, x => x.Characteristics);

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

    /// <summary>
    /// Device changed by the user
    /// </summary>
    private void ChangeDevice(BleDescriptor descriptor) {
        SelectedDevice = null;
        ActiveDevice = descriptor;
        ActiveService = null;
    }
}