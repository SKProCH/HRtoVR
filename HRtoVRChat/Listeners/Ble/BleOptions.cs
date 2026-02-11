using PropertyModels.ComponentModel;

namespace HRtoVRChat.Listeners.Ble;

public class BleOptions : ReactiveObject
{
    public string? SelectedDeviceId { get; set; }
    public string ServiceGuid { get; set; } = "0000180d-0000-1000-8000-00805f9b34fb"; // Heart Rate Service
    public string CharacteristicGuid { get; set; } = "00002a37-0000-1000-8000-00805f9b34fb"; // Heart Rate Measurement
}
