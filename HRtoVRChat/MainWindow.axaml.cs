using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using HRtoVRChat.ViewModels;
using HRtoVRChat_OSC_SDK;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using TextMateSharp.Grammars;

namespace HRtoVRChat;

public partial class MainWindow : Window {
    private readonly TextEditor _textEditor;
    private readonly RichTextModel richTextModel = new();
    private string lastLineColor = "White";

    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        var vm = new MainWindowViewModel();
        DataContext = vm;

        // Setup the Program Output
        _textEditor = this.FindControl<TextEditor>("OutputTextBox");
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textEditor.InstallTextMate(_registryOptions);
        _textEditor.TextArea.TextView.LineTransformers.Add(new RichTextColorizer(richTextModel));

        // Subscribe to events
        vm.OnLogReceived += (message, overrideColor) => {
            Dispatcher.UIThread.InvokeAsync(() => {
                if (overrideColor == "CLEAR") {
                    _textEditor.Clear();
                    return;
                }

                if (message != null && _textEditor != null) {
                    var currentLength = _textEditor.Text?.Length ?? 0;
                    var color = message.Contains("(DEBUG)") ? "DarkGray" :
                        message.Contains("(LOG)") ? "White" :
                        message.Contains("(WARN)") ? "Yellow" :
                        message.Contains("(ERROR)") ? "Red" : lastLineColor;
                    lastLineColor = color;
                    if (!string.IsNullOrEmpty(overrideColor))
                        color = overrideColor;

                    // Simple newline handling logic from original
                    if (currentLength > 0)
                        AppendTextWithColor("\n" + message, Color.Parse(color));
                    else
                        AppendTextWithColor(message, Color.Parse(color));
                    _textEditor.ScrollToEnd();
                }
            });
        };

        // Wire up SoftwareManager UI delegates
        SoftwareManager.ShowMessage = (title, message, isError) => {
            Dispatcher.UIThread.InvokeAsync(() => {
                MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = title,
                    ContentMessage = message,
                    WindowIcon = new WindowIcon(AssetTools.Icon),
                    Icon = isError ? MessageBox.Avalonia.Enums.Icon.Error : MessageBox.Avalonia.Enums.Icon.Info,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                }).Show();
            });
        };

        SoftwareManager.RequestConfirmation = async (title, message) => {
            return await Dispatcher.UIThread.InvokeAsync(async () => {
                var result = await MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentTitle = title,
                    ContentMessage = message,
                    WindowIcon = new WindowIcon(AssetTools.Icon),
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                }).Show();
                return (result & ButtonResult.Yes) != 0;
            });
        };

        vm.RequestHide += Hide;

        // Set Instances
        TrayIconManager.MainWindow = this;
        TrayIconManager.ArgumentsWindow = new Arguments();

        // Check the SetupWizard
        if (!Config.DoesConfigExist()) {
            Dispatcher.UIThread.InvokeAsync(async () => {
                var b = await SetupWizard.AskToSetup();
                if (b) {
                    Hide();
                    new SetupWizard(() => Show()).Show();
                }
            });
        }
    }


    // HUGE thanks to this post
    // https://github.com/icsharpcode/AvalonEdit/issues/244#issuecomment-725214919
    private void AppendTextWithColor(string text, Color color) {
        _textEditor.AppendText(text);
        richTextModel.ApplyHighlighting(_textEditor.Text.Length - text.Length, text.Length,
            new HighlightingColor { Foreground = new SimpleHighlightingBrush(color) });
    }
}
