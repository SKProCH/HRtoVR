using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Tommy.Serializer;

namespace HRtoVRChat;

public class ConfigManager {
    public static readonly string ConfigLocation = Path.Combine(SoftwareManager.OutputPath, "config.cfg");
    public static Config LoadedConfig { get; private set; }
    public static UIConfig LoadedUIConfig { get; private set; }

    public static string UIConfigLocation {
        get {
            if (!string.IsNullOrEmpty(SoftwareManager.LocalDirectory))
                return SoftwareManager.LocalDirectory + "/" + "uiconfig.cfg";
            return "uiconfig.cfg";
        }
    }

    public static void CreateConfig() {
        if (Directory.Exists(SoftwareManager.OutputPath) && File.Exists(ConfigLocation)) {
            // Load
            var nc = TommySerializer.FromTomlFile<Config>(ConfigLocation) ?? new Config();
            //SaveConfig(nc);
            LoadedConfig = nc;
        }
        else
            LoadedConfig = new Config();

        if (File.Exists(UIConfigLocation)) {
            // Load
            var nuic = TommySerializer.FromTomlFile<UIConfig>(UIConfigLocation) ?? new UIConfig();
            //SaveConfig(nc);
            LoadedUIConfig = nuic;
        }
        else
            LoadedUIConfig = new UIConfig();
    }

    public static void SaveConfig(Config config) {
        if (!Directory.Exists(Path.GetDirectoryName(ConfigLocation)))
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigLocation));
        TommySerializer.ToTomlFile(config, ConfigLocation);
    }

    public static void SaveConfig(UIConfig uiConfig) {
        TommySerializer.ToTomlFile(uiConfig, UIConfigLocation);
    }

    private static RadioButton NewRadioButton(MainWindow instance, string name) {
        return new RadioButton {
            Content = name,
            Command = new OnConfigRadioButtonPressed(instance, name),
            GroupName = "ConfigValues"
        };
    }

    public static void InitStackPanels(MainWindow instance) {
        List<string> ConfigValues = new();
        foreach (var fieldInfo in new Config().GetType().GetFields())
            ConfigValues.Add(fieldInfo.Name);
        List<RadioButton> LeftButtonsCreated = new();
        List<RadioButton> RightButtonsCreated = new();
        if (ConfigValues.Count % 2 == 0) {
            var i = 0;
            foreach (var configValue in ConfigValues) {
                var newConfigButton = NewRadioButton(instance, configValue);
                if (i < ConfigValues.Count / 2)
                    LeftButtonsCreated.Add(newConfigButton);
                else
                    RightButtonsCreated.Add(newConfigButton);
                i++;
            }
        }
        else {
            // Treat it as it's even, but it's not
            var fakeEvenTotal = ConfigValues.Count - 1;
            var i = 0;
            while (i < fakeEvenTotal) {
                var newConfigButton = NewRadioButton(instance, ConfigValues[i]);
                if (i < fakeEvenTotal / 2)
                    LeftButtonsCreated.Add(newConfigButton);
                else
                    RightButtonsCreated.Add(newConfigButton);
                i++;
            }

            // Add the final one
            var newConfigButtonOdd = NewRadioButton(instance, ConfigValues[i]);
            RightButtonsCreated.Add(newConfigButtonOdd);
        }

        // Remove Incompatible Config Types
        // L BOZO ENUMERATION WAS MODIFIED
        var side = -1;
        var didError = true;
        var completedLeft = false;
        var completedRight = false;
        while (didError) {
            try {
                if (!completedLeft) {
                    foreach (var radioButton in LeftButtonsCreated) {
                        if (((string)radioButton.Content).Contains("ParameterNames")) {
                            LeftButtonsCreated.Remove(radioButton);
                            side = 0;
                        }

                        completedLeft = true;
                    }
                }

                if (!completedRight) {
                    foreach (var radioButton in RightButtonsCreated) {
                        if (((string)radioButton.Content).Contains("ParameterNames")) {
                            RightButtonsCreated.Remove(radioButton);
                            side = 1;
                        }

                        completedRight = true;
                    }
                }

                didError = false;
            }
            catch (Exception) {
                didError = true;
            }
        }

        // Add buttons
        foreach (var leftRadioButton in LeftButtonsCreated)
            instance.LeftStackPanelConfig.Children.Add(leftRadioButton);
        foreach (var rightRadioButton in RightButtonsCreated)
            instance.RightStackPanelConfig.Children.Add(rightRadioButton);
        // Add the final ParameterNames Button
        var pnButton = new Button {
            Content = "Open ParameterNames Menu",
            Command = new SpecialCommandButton(() => {
                if (!ParameterNames.IsOpen)
                    new ParameterNames().Show();
            })
        };
        switch (side) {
            case 0:
                instance.LeftStackPanelConfig.Children.Add(pnButton);
                break;
            case 1:
                instance.RightStackPanelConfig.Children.Add(pnButton);
                break;
        }
    }
}

public class OnConfigRadioButtonPressed : ICommand {
    public OnConfigRadioButtonPressed(MainWindow Instance, string Name) {
        this.Instance = Instance;
        this.Name = Name;
    }

    public static string SelectedConfigValue { get; private set; }

    private string Name { get; }
    private MainWindow Instance { get; }

    public bool CanExecute(object? parameter) {
        return true;
    }

    public void Execute(object? parameter) {
        var targetField = ConfigManager.LoadedConfig.GetType().GetField(Name);
        var desc = (TommyComment)Attribute.GetCustomAttribute(targetField, typeof(TommyComment));
        Instance.ConfigNameLabel.Text = targetField.Name;
        Instance.ConfigValueType.Text = FriendlyName(targetField.FieldType).ToLower();
        Instance.ConfigDescription.Text = desc.Value;
        SelectedConfigValue = Name;
        if (targetField.GetValue(ConfigManager.LoadedConfig) != null)
            Instance.ConfigValue.Text = targetField.GetValue(ConfigManager.LoadedConfig).ToString();
    }

    // END CREDITS

    public event EventHandler? CanExecuteChanged = (sender, args) => { };

    // BEGIN CREDITS

    /*
     * ToCsv and FriendlyName Methods made by Phil
     * Changes were made to remove this in parameters for both Methods
     * https://stackoverflow.com/a/34001032/12968919
     */

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

public class SpecialCommandButton : ICommand {
    private readonly Action OnClick;

    public SpecialCommandButton(Action OnClick) {
        this.OnClick = OnClick;
    }

    public bool CanExecute(object? parameter) {
        return true;
    }

    public void Execute(object? parameter) {
        OnClick.Invoke();
    }

    public event EventHandler? CanExecuteChanged = (sender, args) => { };
}

[TommyTableName("HRtoVRChat_OSC")]
public class Config {
    [TommyComment("Allow HRtoVRChat_OSC to be used with ChilloutVR. Requires an OSC mod for ChilloutVR")] [TommyInclude]
    public bool ExpandCVR = true;

    [TommyComment("(FitbitHRtoWS Only) The WebSocket to listen to data")] [TommyInclude]
    public string fitbitURL = "ws://localhost:8080/";

    [TommyComment("(HRProxy Only) The code to pull HRProxy Data from")] [TommyInclude]
    public string hrproxyId = string.Empty;

    [TommyComment("The source from where to pull Heart Rate Data")] [TommyInclude]
    public string hrType = "unknown";

    [TommyComment("(HypeRate Only) The code to pull HypeRate Data from")] [TommyInclude]
    public string hyperateSessionId = string.Empty;

    [TommyComment("The IP to send messages to")] [TommyInclude]
    public string ip = "127.0.0.1";

    [TommyComment("The maximum HR for HRPercent")] [TommyInclude]
    public double MaxHR = 255;

    [TommyComment("The minimum HR for HRPercent")] [TommyInclude]
    public double MinHR = 0;

    [TommyComment(
        "A dictionary containing what names to use for default parameters. DON'T CHANGE THE KEYS, CHANGE THE VALUES!")]
    [TommyInclude]
    public Dictionary<string, string> ParameterNames = new() {
        ["onesHR"] = "onesHR",
        ["tensHR"] = "tensHR",
        ["hundredsHR"] = "hundredsHR",
        ["isHRConnected"] = "isHRConnected",
        ["isHRActive"] = "isHRActive",
        ["isHRBeat"] = "isHRBeat",
        ["HRPercent"] = "HRPercent",
        ["FullHRPercent"] = "FullHRPercent",
        ["HR"] = "HR"
    };

    [TommyComment("The Port to send messages to")] [TommyInclude]
    public int port = 9000;

    [TommyComment("(PulsoidSocket Only) The key for the OAuth API to pull HeartRate Data from")] [TommyInclude]
    public string pulsoidkey = string.Empty;

    [TommyComment("(Pulsoid Only) The widgetId to pull HeartRate Data from")] [TommyInclude]
    public string pulsoidwidget = string.Empty;

    [TommyComment("The Port to receive messages from")] [TommyInclude]
    public int receiverPort = 9001;

    [TommyComment("(Stromno Only) The widgetId to pull HeartRate Data from Stromno")] [TommyInclude]
    public string stromnowidget = string.Empty;

    [TommyComment("(TextFile Only) The location of the text file to pull HeartRate Data from")] [TommyInclude]
    public string textfilelocation = string.Empty;

    public static bool DoesConfigExist() {
        return File.Exists(ConfigManager.ConfigLocation);
    }
}

[TommyTableName("HRtoVRChat")]
public class UIConfig {
    [TommyComment("Automatically Start HRtoVRChat_OSC when VRChat is detected")] [TommyInclude]
    public bool AutoStart;

    [TommyComment("Broadcast data over a WebSocket designed for Neos")] [TommyInclude]
    public bool NeosBridge;

    [TommyInclude] public string OtherArgs = "";

    [TommyComment("Force HRtoVRChat_OSC to run whether or not VRChat is detected")] [TommyInclude]
    public bool SkipVRCCheck;

    [TommyComment("Cast Parameter Bools to Floats")] [TommyInclude]
    public bool UseLegacyBool;
}