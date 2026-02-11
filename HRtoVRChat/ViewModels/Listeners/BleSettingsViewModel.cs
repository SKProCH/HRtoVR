using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HRtoVRChat.Infrastructure.Options;
using HRtoVRChat.Listeners.Ble;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels.Listeners;

public class BleSettingsViewModel : ViewModelBase, IListenerSettingsViewModel
{
    private readonly IOptionsManager<BleOptions> _optionsManager;
    private readonly IAdapter _adapter;

    [Reactive] public bool IsScanning { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    public ObservableCollection<BleDevice> DiscoveredDevices { get; } = new();
    [Reactive] public BleDevice? SelectedDevice { get; set; }

    public ObservableCollection<IService> Services { get; } = new();
    [Reactive] public IService? SelectedService { get; set; }

    public ObservableCollection<ICharacteristic> Characteristics { get; } = new();
    [Reactive] public ICharacteristic? SelectedCharacteristic { get; set; }

    public ReactiveCommand<Unit, Unit> ScanCommand { get; }
    public ReactiveCommand<BleDevice, Unit> ConnectCommand { get; }

    public BleSettingsViewModel(IOptionsManager<BleOptions> optionsManager)
    {
        _optionsManager = optionsManager;
        _adapter = CrossBluetoothLE.Current.Adapter;

        ScanCommand = ReactiveCommand.CreateFromTask(ScanAsync);
        ConnectCommand = ReactiveCommand.CreateFromTask<BleDevice>(ConnectAsync);

        this.WhenAnyValue(x => x.SelectedService)
            .WhereNotNull()
            .SelectMany(LoadCharacteristicsAsync)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedCharacteristic)
            .Subscribe(c =>
            {
                if (c != null)
                {
                    _optionsManager.CurrentValue.CharacteristicGuid = c.Id.ToString();
                    _optionsManager.Save();
                }
            });

        this.WhenAnyValue(x => x.SelectedService)
            .Subscribe(s =>
            {
                if (s != null)
                {
                    _optionsManager.CurrentValue.ServiceGuid = s.Id.ToString();
                    _optionsManager.Save();
                }
            });
    }

    private async Task ScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        DiscoveredDevices.Clear();

        try
        {
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            await _adapter.StartScanningForDevicesAsync();
            await Task.Delay(5000); // Scan for 5 seconds
            await _adapter.StopScanningForDevicesAsync();
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            IsScanning = false;
        }
    }

    private void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
    {
        if (DiscoveredDevices.All(d => d.Id != e.Device.Id.ToString()))
        {
            DiscoveredDevices.Add(new BleDevice
            {
                Id = e.Device.Id.ToString(),
                Name = e.Device.Name ?? "Unknown Device"
            });
        }
    }

    private async Task ConnectAsync(BleDevice device)
    {
        try
        {
            var bluetoothDevice = _adapter.DiscoveredDevices.FirstOrDefault(d => d.Id.ToString() == device.Id);
            if (bluetoothDevice == null) return;

            await _adapter.ConnectToDeviceAsync(bluetoothDevice);
            IsConnected = true;

            _optionsManager.CurrentValue.SelectedDeviceId = device.Id;
            _optionsManager.Save();

            await LoadServicesAsync(bluetoothDevice);
        }
        catch (Exception)
        {
            IsConnected = false;
        }
    }

    private async Task LoadServicesAsync(IDevice device)
    {
        Services.Clear();
        var services = await device.GetServicesAsync();
        foreach (var service in services)
        {
            Services.Add(service);
        }

        SelectedService = Services.FirstOrDefault(s => s.Id.ToString() == _optionsManager.CurrentValue.ServiceGuid);
    }

    private async Task<Unit> LoadCharacteristicsAsync(IService service)
    {
        Characteristics.Clear();
        var characteristics = await service.GetCharacteristicsAsync();
        foreach (var characteristic in characteristics)
        {
            Characteristics.Add(characteristic);
        }

        SelectedCharacteristic = Characteristics.FirstOrDefault(c => c.Id.ToString() == _optionsManager.CurrentValue.CharacteristicGuid);
        return Unit.Default;
    }
}
