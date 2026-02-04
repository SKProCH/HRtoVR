using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HRtoVRChat_OSC_SDK;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tommy.Serializer;

namespace HRtoVRChat.ViewModels;

public enum ProgramPanels
{
    Home,
    Program,
    Updates,
    Config,
    IncomingData
}

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ProgramPanels CurrentPanel { get; set; }

    // Status
    [Reactive] public string StatusText { get; set; } = "STOPPED";
    [Reactive] public string AppBridgeStatus { get; set; } = "Not Connected";

    // Updates
    [Reactive] public string InstalledVersion { get; set; } = "";
    [Reactive] public string LatestVersion { get; set; } = "";
    [Reactive] public string UpdateButtonText { get; set; } = "UPDATE SOFTWARE";
    [Reactive] public double TotalProgress { get; set; }
    [Reactive] public double TaskProgress { get; set; }

    // Incoming Data
    [Reactive] public string IncomingDataOutput { get; set; } = "";

    // Command Input
    [Reactive] public string CommandInput { get; set; } = "";

    // Config
    public ObservableCollection<ConfigItemViewModel> ConfigItemsLeft { get; } = new();
    public ObservableCollection<ConfigItemViewModel> ConfigItemsRight { get; } = new();

    [Reactive] public ConfigItemViewModel? SelectedConfigItem { get; set; }
    [Reactive] public string ConfigValueInput { get; set; } = "";

    // Commands
    public ReactiveCommand<ProgramPanels, Unit> SwitchPanelCommand { get; }
    public ReactiveCommand<Unit, Unit> StartCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<Unit, Unit> KillCommand { get; }
    public ReactiveCommand<Unit, Unit> SendCommandCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateSoftwareCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    public ReactiveCommand<string, Unit> OpenUrlCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenArgumentsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenParameterNamesCommand { get; }
    public ReactiveCommand<Unit, Unit> HideAppCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitAppCommand { get; }
    public ReactiveCommand<ConfigItemViewModel, Unit> SwitchConfigSelectionCommand { get; }

    // Events
    public event Action<string, string>? OnLogReceived;
    public event Action? RequestHide;

    private CancellationTokenSource? _cancellationTokenSource;
    private AppBridge? _appBridge;

    public MainWindowViewModel()
    {
        SwitchPanelCommand = ReactiveCommand.Create<ProgramPanels>(p => CurrentPanel = p);

        StartCommand = ReactiveCommand.Create(StartSoftware);
        StopCommand = ReactiveCommand.Create(StopSoftware);
        KillCommand = ReactiveCommand.Create(KillSoftware);
        SendCommandCommand = ReactiveCommand.Create(SendCommand);
        RefreshUpdatesCommand = ReactiveCommand.Create(RefreshUpdates);
        UpdateSoftwareCommand = ReactiveCommand.Create(UpdateSoftware);
        SaveConfigCommand = ReactiveCommand.Create(SaveConfig);
        OpenUrlCommand = ReactiveCommand.Create<string>(OpenBrowser);
        OpenArgumentsCommand = ReactiveCommand.Create(() => TrayIconManager.ArgumentsWindow?.Show());
        OpenParameterNamesCommand = ReactiveCommand.Create(() => {
            if (!ParameterNames.IsOpen)
                new ParameterNames().Show();
        });
        SwitchConfigSelectionCommand = ReactiveCommand.Create<ConfigItemViewModel>(item => {
            SelectedConfigItem = item;
        });
        HideAppCommand = ReactiveCommand.Create(() =>
        {
             TrayIconManager.Update(new TrayIconManager.UpdateTrayIconInformation { HideApplication = true });
             RequestHide?.Invoke();
        });
        ExitAppCommand = ReactiveCommand.Create(() =>
        {
            KillSoftware();
            Environment.Exit(0);
        });

        // Subscriptions
        this.WhenAnyValue(x => x.SelectedConfigItem)
            .Where(x => x != null)
            .Subscribe(item => {
                if (item != null)
                {
                    var targetField = ConfigManager.LoadedConfig.GetType().GetField(item.FieldName);
                    if (targetField != null)
                    {
                        var val = targetField.GetValue(ConfigManager.LoadedConfig);
                        ConfigValueInput = val?.ToString() ?? "";
                    }
                }
            });

        Initialize();
    }

    private void Initialize()
    {
        if (!string.IsNullOrEmpty(SoftwareManager.LocalDirectory) && !Directory.Exists(SoftwareManager.LocalDirectory))
            Directory.CreateDirectory(SoftwareManager.LocalDirectory);

        ConfigManager.CreateConfig();
        LoadConfigItems();

        SoftwareManager.OnConsoleUpdate += (message, overrideColor) =>
        {
             // Marshaling to UI thread is handled by View's subscription or Dispatcher within View
             // But let's invoke event safely
             OnLogReceived?.Invoke(message ?? "", overrideColor);
        };

        SoftwareManager.RequestUpdateProgressBars += (x, y) =>
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                TotalProgress = x;
                TaskProgress = y;
            });
        };

        RefreshUpdates();
        if (!SoftwareManager.IsInstalled)
            UpdateButtonText = "INSTALL SOFTWARE";

        StartBackgroundThread();
    }

    private void LoadConfigItems()
    {
        var configValues = new List<string>();
        foreach (var fieldInfo in new Config().GetType().GetFields())
            configValues.Add(fieldInfo.Name);

        var allItems = new List<ConfigItemViewModel>();

        foreach (var configValue in configValues)
        {
             var field = new Config().GetType().GetField(configValue);
             if (field == null) continue;

             // Skip ParameterNames as in original
             if (configValue == "ParameterNames") continue;

             var descAttr = (TommyComment)Attribute.GetCustomAttribute(field, typeof(TommyComment));
             var desc = descAttr?.Value ?? "";

             var item = new ConfigItemViewModel
             {
                 Name = field.Name,
                 FieldName = field.Name,
                 TypeName = FriendlyName(field.FieldType).ToLower(),
                 Description = desc,
                 Value = field.GetValue(ConfigManager.LoadedConfig)
             };
             allItems.Add(item);
        }

        // Split logic roughly based on original even/odd but skipping ParameterNames
        int mid = allItems.Count / 2;
        if (allItems.Count % 2 != 0) mid = (allItems.Count - 1) / 2; // Original logic slightly different

        // Replicating original odd/even logic roughly
        // Original: if even, split half/half. if odd, split (total-1)/2, last one goes to right.

        int count = allItems.Count;
        int leftCount = count / 2;
        // If odd, the extra one went to right in original code (index i >= fakeEvenTotal/2)
        // If count is 5, fakeEvenTotal=4. i<2 -> left. i>=2 -> right. + last -> right.
        // So Left gets 2, Right gets 3.

        for (int i = 0; i < count; i++)
        {
            if (i < leftCount)
                ConfigItemsLeft.Add(allItems[i]);
            else
                ConfigItemsRight.Add(allItems[i]);
        }
    }

    private void StartSoftware()
    {
        // Clear log handled by view via event or manual clear?
        // Original: _textEditor.Clear(); SoftwareManager.OnConsoleUpdate(...)
        // We will send a special signal or just rely on OnConsoleUpdate
        // Let's send a clear signal via event if needed, or just append.
        // The original code clears text editor. We can add a ClearLog event.

        OnLogReceived?.Invoke(null, "CLEAR"); // Use null as signal to clear? Or separate event.

        SoftwareManager.OnConsoleUpdate(
            $"HRtoVRChat_OSC {SoftwareManager.GetInstalledVersion()} Created by 200Tigersbloxed\n", string.Empty);
        SoftwareManager.StartSoftware();
    }

    private void StopSoftware()
    {
        SoftwareManager.StopSoftware();
    }

    private void KillSoftware()
    {
        SoftwareManager.StopSoftware();
        try {
            foreach (var process in Process.GetProcessesByName("HRtoVRChat_OSC")) {
                process.Kill();
            }
        }
        catch (Exception) { }
    }

    private void SendCommand()
    {
        if (!string.IsNullOrEmpty(CommandInput))
        {
            SoftwareManager.SendCommand(CommandInput);
            CommandInput = "";
        }
    }

    private void RefreshUpdates()
    {
        LatestVersion = "Latest Version: " + SoftwareManager.GetLatestVersion();
        InstalledVersion = "Installed Version: " + SoftwareManager.GetInstalledVersion();
    }

    private async void UpdateSoftware()
    {
        await SoftwareManager.InstallSoftware(() => {
            Dispatcher.UIThread.InvokeAsync(() => {
                UpdateButtonText = "UPDATE SOFTWARE";
            });
        });
    }

    private void SaveConfig()
    {
        if (SelectedConfigItem != null)
        {
            var targetField = ConfigManager.LoadedConfig.GetType().GetField(SelectedConfigItem.FieldName);
            if (targetField != null)
            {
                try {
                    targetField.SetValue(ConfigManager.LoadedConfig,
                        Convert.ChangeType(ConfigValueInput, targetField.FieldType));
                    ConfigManager.SaveConfig(ConfigManager.LoadedConfig);
                    // Update the item value
                    SelectedConfigItem.Value = ConfigValueInput;
                } catch { }
            }
        }
    }

    private void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Process.Start("open", url);
        }
        else {
            try {
                // Fallback
                if (url.Contains("github"))
                    Process.Start("https://github.com/200Tigersbloxed/HRtoVRChat_OSC");
            }
            catch (Exception) { }
        }
    }

    private void StartBackgroundThread()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () => {
            var attemptConnect = false;
            while (!_cancellationTokenSource.IsCancellationRequested) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    StatusText = "STATUS: " + (SoftwareManager.IsSoftwareRunning ? "RUNNING" : "STOPPED");
                    AppBridgeStatus = "App Bridge Connection Status: " +
                        (_appBridge?.IsClientConnected ?? false ? "Connected" : "Not Connected");

                    TrayIconManager.Update(new TrayIconManager.UpdateTrayIconInformation {
                        Status = SoftwareManager.IsSoftwareRunning ? "RUNNING" : "STOPPED"
                    });
                });

                if (SoftwareManager.IsSoftwareRunning && !attemptConnect && !(_appBridge?.IsClientConnected ?? false)) {
                    _appBridge = new AppBridge();
                    _appBridge.OnAppBridgeMessage += async message => {
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            var avatarParameters = string.Empty;
                            foreach (var currentAvatarParameter in message.CurrentAvatar?.parameters ?? new List<string>())
                                avatarParameters += currentAvatarParameter + "\n";

                            IncomingDataOutput = $"Current Source: {message.CurrentSourceName}\n\n" +
                                                      "-- Parameters --\n" +
                                                      $"onesHR: {message.onesHR}\n" +
                                                      $"tensHR: {message.tensHR}\n" +
                                                      $"hundredsHR: {message.hundredsHR}\n" +
                                                      $"isHRConnected: {message.isHRConnected}\n" +
                                                      $"isHRActive: {message.isHRActive}\n" +
                                                      $"isHRBeat: {message.isHRBeat} (inaccurate over AppBridge)\n" +
                                                      $"HRPercent: {message.HRPercent}\n" +
                                                      $"FullHRPercent: {message.FullHRPercent}\n" +
                                                      $"HR: {message.HR}\n\n" +
                                                      "-- Current Avatar --\n" +
                                                      $"name: {message.CurrentAvatar?.name ?? "unknown"}\n" +
                                                      $"id: {message.CurrentAvatar?.id ?? "unknown"}\n" +
                                                      "== parameters ==\n" +
                                                      $"{avatarParameters}";
                        });
                    };
                    _appBridge.OnClientDisconnect += async () => {
                        _appBridge.StopClient();
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            IncomingDataOutput = "";
                        });
                    };
                    _appBridge.InitClient();
                    attemptConnect = true;
                }
                else
                    attemptConnect = false;

                Thread.Sleep(10);
            }
        }, _cancellationTokenSource.Token);
    }

    // Helper from original code
    private static string ToCsv(IEnumerable<object> collectionToConvert, string separator = ", ") {
        return string.Join(separator, collectionToConvert.Select(o => o.ToString()));
    }

    private static string FriendlyName(Type type) {
        if (type.IsGenericType) {
            var namePrefix = type.Name.Split(new[] { '`' }, StringSplitOptions.RemoveEmptyEntries)[0];
            var genericParameters = ToCsv(type.GetGenericArguments().Select(FriendlyName));
            return namePrefix + "<" + genericParameters + ">";
        }

        return type.Name;
    }
}
