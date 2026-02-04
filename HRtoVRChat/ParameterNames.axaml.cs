using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace HRtoVRChat;

public partial class ParameterNames : Window {
    public static bool IsOpen;
    public TextBox ParameterNameValue;
    public TextBlock SelectedParameterDescription;

    public TextBlock SelectedParameterName;
    public TextBlock SelectedParameterType;

    public ParameterNames() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        Closed += (sender, args) => IsOpen = false;
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Show() {
        base.Show();
        IsOpen = true;
        // idk why this has to be done JIT, it just throws an error when done pre-compiled
        // CONTENT
        var content = new Canvas();
        Content = content;
        var ParameterNamesStackPanel = new StackPanel();
        ParameterNamesStackPanel.SetValue(Canvas.TopProperty, 5);
        ParameterNamesStackPanel.SetValue(Canvas.LeftProperty, 5);
        content.Children.Add(ParameterNamesStackPanel);
        // RADIOBUTTONS
        foreach (var keyValuePair in new Config().ParameterNames) {
            var nb = NewRadioButton(keyValuePair.Key);
            ParameterNamesStackPanel.Children.Add(nb);
        }

        // LABELS
        // -- name
        var ParameterNameLabel = new TextBlock {
            Text = "Parameter Name",
            FontSize = 26,
            FontWeight = FontWeight.Bold
        };
        ParameterNameLabel.SetValue(Canvas.LeftProperty, 250);
        ParameterNameLabel.SetValue(Canvas.TopProperty, 6);
        content.Children.Add(ParameterNameLabel);
        SelectedParameterName = new TextBlock {
            Text = "Select a Parameter",
            FontSize = 16
        };
        SelectedParameterName.SetValue(Canvas.LeftProperty, 250);
        SelectedParameterName.SetValue(Canvas.TopProperty, 56);
        content.Children.Add(SelectedParameterName);
        // -- type
        var ParameterTypeLabel = new TextBlock {
            Text = "Parameter Type",
            FontSize = 26,
            FontWeight = FontWeight.Bold
        };
        ParameterTypeLabel.SetValue(Canvas.LeftProperty, 250);
        ParameterTypeLabel.SetValue(Canvas.TopProperty, 96);
        content.Children.Add(ParameterTypeLabel);
        SelectedParameterType = new TextBlock {
            Text = "unknown",
            FontSize = 16
        };
        SelectedParameterType.SetValue(Canvas.LeftProperty, 250);
        SelectedParameterType.SetValue(Canvas.TopProperty, 146);
        content.Children.Add(SelectedParameterType);
        // -- description
        var ParameterDescriptionLabel = new TextBlock {
            Text = "Parameter Description",
            FontSize = 26,
            FontWeight = FontWeight.Bold
        };
        ParameterDescriptionLabel.SetValue(Canvas.LeftProperty, 250);
        ParameterDescriptionLabel.SetValue(Canvas.TopProperty, 186);
        content.Children.Add(ParameterDescriptionLabel);
        SelectedParameterDescription = new TextBlock {
            Text = "Description",
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            Width = 350
        };
        SelectedParameterDescription.SetValue(Canvas.LeftProperty, 250);
        SelectedParameterDescription.SetValue(Canvas.TopProperty, 236);
        content.Children.Add(SelectedParameterDescription);
        // -- TextBox
        ParameterNameValue = new TextBox {
            Width = 600,
            Watermark = "Insert a new Parameter Name",
            TextAlignment = TextAlignment.Center
        };
        ParameterNameValue.SetValue(Canvas.TopProperty, 420);
        ParameterNameValue.SetValue(Canvas.LeftProperty, 5);
        content.Children.Add(ParameterNameValue);
        // -- Button
        var ApplyButton = new Button {
            Content = new TextBlock {
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Text = "SAVE"
            },
            Width = 600,
            Command = new ApplyButtonClicked(this)
        };
        ApplyButton.SetValue(Canvas.TopProperty, 460);
        ApplyButton.SetValue(Canvas.LeftProperty, 5);
        content.Children.Add(ApplyButton);
    }

    private RadioButton NewRadioButton(string name) {
        return new RadioButton {
            Content = name,
            Command = new ParameterNameRadioButtonSelected(this, name),
            GroupName = "ParameterNameConfigValues"
        };
    }

    private class ParameterNameRadioButtonSelected : ICommand {
        private readonly ParameterNames instance;
        private readonly string Name;

        public ParameterNameRadioButtonSelected(ParameterNames instance, string Name) {
            this.Name = Name;
            this.instance = instance;
        }

        public bool CanExecute(object? parameter) {
            return true;
        }

        public void Execute(object? parameter) {
            // Get the Parameter Name, Type, and Description
            string value;
            ConfigManager.LoadedConfig.ParameterNames.TryGetValue(Name, out value);
            if (!string.IsNullOrEmpty(value)) {
                instance.SelectedParameterName.Text = Name;
                try {
                    var pd = ParameterData.ParameterDatas[Name];
                    instance.SelectedParameterType.Text = pd.type;
                    instance.SelectedParameterDescription.Text = pd.description;
                }
                catch (Exception) {
                    MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                        ButtonDefinitions = ButtonEnum.YesNo,
                        ContentTitle = "ParameterNames",
                        ContentMessage = "Failed to get ParameterData for parameter " + Name,
                        WindowIcon = new WindowIcon(AssetTools.Icon),
                        Icon = MessageBox.Avalonia.Enums.Icon.Error,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    }).Show();
                }

                instance.ParameterNameValue.Text = value;
            }
        }

        public event EventHandler? CanExecuteChanged = (sender, args) => { };
    }

    private class ApplyButtonClicked : ICommand {
        private readonly ParameterNames instance;

        public ApplyButtonClicked(ParameterNames instance) {
            this.instance = instance;
        }

        public bool CanExecute(object? parameter) {
            return true;
        }

        public void Execute(object? parameter) {
            // try and set the value
            try {
                ConfigManager.LoadedConfig.ParameterNames[instance.SelectedParameterName.Text] =
                    instance.ParameterNameValue.Text;
                ConfigManager.SaveConfig(ConfigManager.LoadedConfig);
            }
            catch (Exception) {
                MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentTitle = "ParameterNames",
                    ContentMessage = "Failed to save Parameter " + instance.SelectedParameterName.Text + "!",
                    WindowIcon = new WindowIcon(AssetTools.Icon),
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                }).Show();
            }
        }

        public event EventHandler? CanExecuteChanged = (sender, args) => { };
    }

    public class ParameterData {
        public static readonly Dictionary<string, ParameterData> ParameterDatas = new() {
            ["onesHR"] = new ParameterData {
                type = "int",
                description = "Ones spot in the Heart Rate reading; 12**3** *(legacy)*"
            },
            ["tensHR"] = new ParameterData {
                type = "int",
                description = "Tens spot in the Heart Rate reading; 1**2**3 *(legacy)*"
            },
            ["hundredsHR"] = new ParameterData {
                type = "int",
                description = "Hundreds spot in the Heart Rate reading; **1**23 *(legacy)*"
            },
            ["isHRConnected"] = new ParameterData {
                type = "bool",
                description = "Returns whether the device's connection is valid or not"
            },
            ["isHRActive"] = new ParameterData {
                type = "bool",
                description = "Returns whether the connection is valid or not"
            },
            ["isHRBeat"] = new ParameterData {
                type = "bool",
                description = "Estimation on when the heart is beating"
            },
            ["HRPercent"] = new ParameterData {
                type = "float",
                description = "Range of HR between the MinHR and MaxHR config value"
            },
            ["FullHRPercent"] = new ParameterData {
                type = "float",
                description = "Range of HR between the MinHR and the MaxHR config value, from -1 to 1"
            },
            ["HR"] = new ParameterData {
                type = "int",
                description = "Returns the raw HR, ranged from 0 - 255. *(required)*"
            }
        };

        public string description;

        public string type;
    }
}