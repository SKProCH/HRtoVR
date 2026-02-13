using System;

namespace HRtoVRChat.Listeners.Ble;

/// <summary>
/// Represents a Bluetooth Low Energy (BLE) device, service or characteristic.
/// </summary>
public record BleDescriptor(Guid Id, string Name);
