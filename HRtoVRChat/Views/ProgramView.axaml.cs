using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using HRtoVRChat.Utils;
using HRtoVRChat.ViewModels;
using TextMateSharp.Grammars;

namespace HRtoVRChat.Views
{
    public partial class ProgramView : UserControl
    {
        private readonly TextEditor _textEditor;
        private readonly HRtoVRChat.Utils.RichTextModel richTextModel = new();
        private string lastLineColor = "White";

        public ProgramView()
        {
            InitializeComponent();

            // Setup the Program Output
            _textEditor = this.FindControl<TextEditor>("OutputTextBox");
            if (_textEditor != null)
            {
                var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
                _textEditor.InstallTextMate(_registryOptions);
                _textEditor.TextArea.TextView.LineTransformers.Add(new HRtoVRChat.Utils.RichTextColorizer(richTextModel));
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is ProgramViewModel vm)
            {
                vm.OnLogReceived += OnLogReceived;
                // Update status immediately on attach
                vm.UpdateStatus();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLogReceived(string? message, string overrideColor)
        {
            Dispatcher.UIThread.InvokeAsync(() => {
                if (_textEditor == null) return;

                if (overrideColor == "CLEAR") {
                    _textEditor.Clear();
                    return;
                }

                if (message != null) {
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
        }

        // HUGE thanks to this post
        // https://github.com/icsharpcode/AvalonEdit/issues/244#issuecomment-725214919
        private void AppendTextWithColor(string text, Color color) {
            _textEditor.AppendText(text);
            richTextModel.ApplyHighlighting(_textEditor.Text.Length - text.Length, text.Length,
                new HighlightingColor { Foreground = new SimpleHighlightingBrush(color) });
        }
    }
}
