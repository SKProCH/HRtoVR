using System;

namespace HRtoVRChat.Listeners.Ble;

public record BleCharacteristic(Guid Id, string Name, bool CanUpdate) : BleDescriptor(Id, Name);