using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using HRtoVRChat_OSC_SDK;
using SuperSimpleTcp;

namespace HRtoVRChat.HRManagers;

public class SDKManager : HRManager {
    public static string PreferredSDK = string.Empty;

    public static readonly string SDKsLocation = "SDKs";
    private readonly Dictionary<ExternalHRSDK, Messages.HRMessage> ExternalHrsdks = new();
    private readonly Dictionary<HRSDK, Messages.HRMessage> Hrsdks = new();
    private readonly Dictionary<string, Messages.HRMessage> RemoteSDKs = new();

    private Thread? _worker;

    private SimpleTcpServer? server;
    private CancellationTokenSource? token;

    public bool Init(string d1) {
        if (_worker != null) {
            token?.Cancel();
        }

        token = new CancellationTokenSource();
        _worker = new Thread(() => {
            server = new SimpleTcpServer(d1);
            server.Events.ClientConnected += (sender, args) => {
                RemoteSDKs.Add(args.IpPort, new Messages.HRMessage());
                LogHelper.Log("SDK Connected!");
            };
            server.Events.ClientDisconnected += (sender, args) => {
                try {
                    RemoteSDKs.Remove(args.IpPort);
                }
                catch (Exception e) { LogHelper.Error("Failed to remove RemoteSDK!", e); }

                LogHelper.Log("SDK Disconnected!");
            };
            server.Events.DataReceived += (sender, args) => {
                var data = args.Data;
                var fakeDeserialize = Messages.DeserializeMessage(data);
                var messageType = Messages.GetMessageType(fakeDeserialize);
                switch (messageType) {
                    case "HRMessage":
                        var hrm = Messages.DeserializeMessage<Messages.HRMessage>(data);
                        foreach (var remoteSdK in RemoteSDKs) {
                            if (remoteSdK.Key == args.IpPort)
                                RemoteSDKs[remoteSdK.Key] = hrm;
                        }

                        break;
                    case "HRLogMessage":
                        var hrLogMessage = Messages.DeserializeMessage<Messages.HRLogMessage>(data);
                        switch (hrLogMessage.LogLevel) {
                            case HRSDK.LogLevel.Debug:
                                LogHelper.Debug(hrLogMessage.Message);
                                break;
                            case HRSDK.LogLevel.Log:
                                LogHelper.Log(hrLogMessage.Message, hrLogMessage.Color);
                                break;
                            case HRSDK.LogLevel.Warn:
                                LogHelper.Log(hrLogMessage.Message);
                                break;
                            case HRSDK.LogLevel.Error:
                                LogHelper.Error(hrLogMessage.Message);
                                break;
                        }

                        break;
                    case "GetHRData":
                        var hrm_ghrd = new Messages.HRMessage {
                            SDKName = GetPreferredHRData()?.SDKName ?? "unknown",
                            HR = GetHR(),
                            IsActive = IsActive(),
                            IsOpen = IsOpen()
                        };
                        server.Send(args.IpPort, hrm_ghrd.Serialize());
                        break;
                    default:
                        LogHelper.Warn("Unknown Debug Message: " + messageType);
                        break;
                }
            };
            server.Start();
            LogHelper.Debug("Started SDK Server at " + d1);
            SDKPatches.OnHRData += message => {
                if (message.IsActive)
                    SetHRDataBySDKName(message.SDKName, message);
            };
            SDKPatches.OnRequestData += sdk => {
                var hrm = new Messages.HRMessage {
                    HR = GetHR(),
                    IsActive = IsActive(),
                    IsOpen = IsOpen()
                };
                SendDataToSDK(sdk, hrm);
            };
            foreach (var file in Directory.GetFiles(SDKsLocation, "*.dll")) {
                // Attempt to load the file
                try {
                    var assembly = Assembly.LoadFile(Path.GetFullPath(file));
                    var externalHrsdks =
                        assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ExternalHRSDK))).ToList();
                    var HRSDKs = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(HRSDK))).ToList();
                    // ExternalHRSDK (Obsolete)
                    foreach (var externalHrsdk in externalHrsdks) {
                        try {
                            var loaded = (ExternalHRSDK)Activator.CreateInstance(externalHrsdk);
                            if (loaded != null) {
                                if (loaded.Initialize() || loaded.OverrideInitializeAdd) {
                                    LogHelper.Log("Loaded ExternalHRSDK " + loaded.SDKName);
                                    ExternalHrsdks.Add(loaded, loaded.CurrentHRData);
                                    loaded.OnHRMessageUpdated += message => {
                                        if (message.IsActive)
                                            SetHRDataBySDKName(message.SDKName, message);
                                    };
                                }
                                else
                                    LogHelper.Error(loaded.SDKName + " failed to initialize!");
                            }
                            else
                                LogHelper.Error("Failed to create an ExternalHRSDK under the file " + file);
                        }
                        catch (Exception e) {
                            LogHelper.Error("Unknown Error while loading an ExternalHRSDK from file" + file, e);
                        }
                    }

                    // HRSDK
                    foreach (var hrsdk in HRSDKs) {
                        try {
                            var loaded = (HRSDK)Activator.CreateInstance(hrsdk);
                            if (loaded != null) {
                                // Reflection
                                loaded.GetType().BaseType
                                    .GetField("_isReflected", BindingFlags.NonPublic | BindingFlags.Instance)
                                    .SetValue(loaded, true);
                                // Harmony Patches
                                var sdkPatch = new Harmony(loaded.Options.SDKName + "-patch");
                                sdkPatch.Patch(
                                    typeof(HRSDK).GetMethod("on_push_data",
                                        BindingFlags.Instance | BindingFlags.NonPublic),
                                    new HarmonyMethod(typeof(SDKPatches).GetMethod("on_push_data",
                                        BindingFlags.Static | BindingFlags.NonPublic)));
                                sdkPatch.Patch(
                                    typeof(HRSDK).GetMethod("on_pull_data",
                                        BindingFlags.Instance | BindingFlags.NonPublic),
                                    new HarmonyMethod(typeof(SDKPatches).GetMethod("on_pull_data",
                                        BindingFlags.Static | BindingFlags.NonPublic)));
                                sdkPatch.Patch(
                                    typeof(HRSDK).GetMethod("on_log",
                                        BindingFlags.Instance | BindingFlags.NonPublic),
                                    new HarmonyMethod(typeof(SDKPatches).GetMethod("on_log",
                                        BindingFlags.Static | BindingFlags.NonPublic)));
                                LogHelper.Log("Loaded HRSDK " + loaded.Options.SDKName);
                                Hrsdks.Add(loaded, new Messages.HRMessage { SDKName = loaded.Options.SDKName });
                                loaded.OnSDKOpened();
                            }
                            else
                                LogHelper.Error("Failed to create an HRSDK under the file " + file);
                        }
                        catch (Exception e) {
                            LogHelper.Error("Unknown Error while loading an HRSDK from file" + file, e);
                        }
                    }
                }
                catch (Exception ee) {
                    LogHelper.Error("Unknown Exception while processing an HRSDK from file " + file, ee);
                }
            }

            LogHelper.Debug("Finished loading all HRSDKs");
            var networked_update = 0;
            while (!token.IsCancellationRequested) {
                var c = 0;
                foreach (var client in server.GetClients()) {
                    try {
                        if (networked_update >= 10) {
                            server.Send(client, new Messages.UpdateMessage().Serialize());
                            networked_update = 0;
                        }
                        else
                            networked_update++;

                        c++;
                    }
                    catch (Exception) { LogHelper.Warn("Socket Connection was closed when sending update!"); }
                }

                foreach (var externalHrsdk in ExternalHrsdks) {
                    // Update first
                    externalHrsdk.Key.Update();
                    // THEN do checks
                    if (externalHrsdk.Key.CurrentHRData.IsActive)
                        c++;
                }

                foreach (var hrsdk in Hrsdks) {
                    // Update first
                    hrsdk.Key.OnSDKUpdate();
                    // THEN do checks
                    if (hrsdk.Key.IsActive)
                        c++;
                }

                Thread.Sleep(10);
            }

            server?.Stop();
            foreach (var hrsdk in Hrsdks)
                hrsdk.Key.OnSDKClosed();
            Hrsdks.Clear();
            foreach (var externalHrsdk in ExternalHrsdks)
                externalHrsdk.Key.Destroy();
            ExternalHrsdks.Clear();
        });
        _worker.Start();
        return true;
    }

    public string GetName() {
        var hrm = GetPreferredHRData();
        return hrm?.SDKName ?? "sdk";
    }

    public int GetHR() {
        return GetPreferredHRData()?.HR ?? 0;
    }

    public void Stop() {
        token?.Cancel();
    }

    public bool IsOpen() {
        return GetPreferredHRData()?.IsOpen ?? false;
    }

    public bool IsActive() {
        return GetPreferredHRData()?.IsActive ?? false;
    }

    private void SetHRDataBySDKName(string name, Messages.HRMessage hrm) {
        // Remote
        foreach (var keyValuePair in RemoteSDKs) {
            if (keyValuePair.Value.SDKName == name)
                RemoteSDKs[keyValuePair.Key] = hrm;
        }

        // ExternalHRSDK (Obsolete)
        foreach (var externalHrsdk in ExternalHrsdks) {
            if (externalHrsdk.Key.SDKName == name)
                ExternalHrsdks[externalHrsdk.Key] = hrm;
        }

        // HRSDK
        foreach (var hrsdk in Hrsdks) {
            if (hrsdk.Key.Options.SDKName == name)
                Hrsdks[hrsdk.Key] = hrm;
        }
    }

    private void SendDataToSDK(string name, Messages.HRMessage data) {
        // Remote
        foreach (var keyValuePair in RemoteSDKs) {
            if (keyValuePair.Value.SDKName == name) {
                // Serialize
                var serialize = data.Serialize();
                // Send
                server.Send(keyValuePair.Key, serialize);
            }
        }

        // ExternalHRSDK (Obsolete) is not supported
        // HRSDK
        foreach (var hrsdk in Hrsdks) {
            if (hrsdk.Key.Options.SDKName == name)
                hrsdk.Key.OnSDKData(data);
        }
    }

    private Messages.HRMessage? GetRemoteHRDataBySDKName(string name) {
        // Remote
        foreach (var keyValuePair in RemoteSDKs) {
            if (keyValuePair.Value.SDKName == name)
                return RemoteSDKs[keyValuePair.Key];
        }

        // ExternalHRSDK (Obsolete)
        foreach (var externalHrsdk in ExternalHrsdks) {
            if (externalHrsdk.Key.SDKName == name)
                return ExternalHrsdks[externalHrsdk.Key];
        }

        // HRSDK
        foreach (var hrsdk in Hrsdks) {
            if (hrsdk.Key.Options.SDKName == name)
                return Hrsdks[hrsdk.Key];
        }

        return null;
    }

    private Messages.HRMessage? GetPreferredHRData() {
        if (!string.IsNullOrEmpty(PreferredSDK)) {
            var target = GetRemoteHRDataBySDKName(PreferredSDK);
            if (target != null)
                return target;
        }

        // If the above is false, or returns null, then select automatically
        // Prefer Internal
        foreach (var keyValuePair in Hrsdks) {
            if (keyValuePair.Value.IsActive)
                return keyValuePair.Value;
        }

        foreach (var keyValuePair in ExternalHrsdks) {
            if (keyValuePair.Value.IsActive)
                return keyValuePair.Value;
        }

        // Then Remote
        foreach (var keyValuePair in RemoteSDKs) {
            if (keyValuePair.Value.IsActive)
                return keyValuePair.Value;
        }

        return null;
    }

    public void DestroySDKByName(string name) {
        // Remote
        foreach (var keyValuePair in RemoteSDKs) {
            if (keyValuePair.Value.SDKName == name) {
                server.DisconnectClient(keyValuePair.Key);
                LogHelper.Debug("Disconnected Remote HRSDK " + name);
            }
        }

        // ExternalHRSDK (Obsolete)
        foreach (var externalHrsdk in ExternalHrsdks) {
            if (externalHrsdk.Key.SDKName == name) {
                externalHrsdk.Key.Destroy();
                try {
                    ExternalHrsdks.Remove(externalHrsdk.Key);
                    LogHelper.Debug("Destroyed ExternalHRSDK " + name);
                }
                catch (Exception e) {
                    LogHelper.Error("Failed to Dispose ExternalHRSDK " + externalHrsdk.Key.SDKName + "!", e);
                }
            }
        }

        // HRSDK
        foreach (var hrsdk in Hrsdks) {
            if (hrsdk.Key.Options.SDKName == name) {
                hrsdk.Key.OnSDKClosed();
                try {
                    Hrsdks.Remove(hrsdk.Key);
                    LogHelper.Debug("Disconnected HRSDK " + name);
                }
                catch (Exception e) {
                    LogHelper.Error("Failed to Dispose ExternalHRSDK " + hrsdk.Key.Options.SDKName + "!", e);
                }
            }
        }
    }
}

public class SDKPatches {
    public static Action<Messages.HRMessage> OnHRData = message => { };
    public static Action<string> OnRequestData = requesting_sdk => { };

    private static void on_push_data(Messages.HRMessage hrm) {
        OnHRData.Invoke(hrm);
    }

    private static void on_pull_data(string requesting_sdk) {
        OnRequestData.Invoke(requesting_sdk);
    }

    private static void on_log(HRSDK instance, HRSDK.LogLevel logLevel, object msg,
        ConsoleColor color = ConsoleColor.White, Exception? e = null) {
        switch (logLevel) {
            case HRSDK.LogLevel.Debug:
                LogHelper.Debug($"[SDK : {instance.Options.SDKName}] {msg}");
                break;
            case HRSDK.LogLevel.Log:
                LogHelper.Log($"[SDK : {instance.Options.SDKName}] {msg}", color);
                break;
            case HRSDK.LogLevel.Warn:
                LogHelper.Warn($"[SDK : {instance.Options.SDKName}] {msg}");
                break;
            case HRSDK.LogLevel.Error:
                if (e != null)
                    LogHelper.Error($"[SDK : {instance.Options.SDKName}] {msg}", e);
                else
                    LogHelper.Error($"[SDK : {instance.Options.SDKName}] {msg}");
                break;
        }
    }
}