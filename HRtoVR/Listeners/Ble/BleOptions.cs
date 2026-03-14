using ReactiveUI.Fody.Helpers;
using ReactiveObject = ReactiveUI.ReactiveObject;

namespace HRtoVRChat.Listeners.Ble;

public class BleOptions : ReactiveObject {
    [Reactive] public BleDescriptor? Device { get; set; }
    [Reactive] public BleDescriptor? Service { get; set; }
    [Reactive] public BleDescriptor? Characteristic { get; set; }
}