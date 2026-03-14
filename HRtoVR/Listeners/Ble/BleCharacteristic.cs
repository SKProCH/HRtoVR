using System;

namespace HRtoVR.Listeners.Ble;

public record BleCharacteristic(Guid Id, string Name, bool CanUpdate) : BleDescriptor(Id, Name);