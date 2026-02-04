using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HRtoVRChat.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace HRtoVRChat;

public partial class SetupWizard : Window {
    private readonly Action? onDone;

    public SetupWizard() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        var vm = new SetupWizardViewModel();
        DataContext = vm;
        SetupEvents(vm);
    }

    public SetupWizard(Action onDone) : this() {
        this.onDone = onDone;
    }

    private void SetupEvents(SetupWizardViewModel vm) {
        vm.RequestClose += () => {
            onDone?.Invoke();
            Close();
        };

        vm.RequestShowExtraInfo += (selector, callback) => {
            ShowExtra(selector, callback);
        };
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }

    public static async Task<bool> AskToSetup() {
        var br = await MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams {
            Icon = MessageBox.Avalonia.Enums.Icon.Question,
            WindowIcon = new WindowIcon(AssetTools.Icon),
            ContentTitle = "HRtoVRChat",
            ContentHeader = "No Config was Found!",
            ContentMessage = "Run the SetupWizard?",
            ButtonDefinitions = ButtonEnum.YesNo
        }).Show();
        if ((br & ButtonResult.Yes) != 0)
            return true;
        return false;
    }

    private void ShowExtra(HRTypeSelector selector, Action<List<HRTypeExtraInfo>> onDone) {
        if (selector.ExtraInfos.Count <= 0) {
            onDone.Invoke(new List<HRTypeExtraInfo>());
            return;
        }

        var newWindow = new Window {
            Width = 500,
            Height = 500,
            MaxWidth = 500,
            MaxHeight = 500,
            MinWidth = 500,
            MinHeight = 500,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Icon = new WindowIcon(AssetTools.Icon),
            Title = selector.Name + " Extra"
        };
        var grid = new Grid {
            RowDefinitions = new RowDefinitions("*,Auto")
        };

        var sp = new StackPanel {
            Margin = new Thickness(10)
        };

        var scrollViewer = new ScrollViewer {
            Content = sp
        };
        grid.Children.Add(scrollViewer);

        Dictionary<HRTypeExtraInfo, TextBox> texts = new();
        foreach (var hrTypeExtraInfo in selector.ExtraInfos) {
            var title = new Label {
                Content = hrTypeExtraInfo.name,
                FontSize = 16
            };
            var desc = new Label {
                Content = hrTypeExtraInfo.description
            };
            var tb = new TextBox {
                Watermark = "example: " + hrTypeExtraInfo.example,
                // Width = 490 // Let layout handle width
            };
            texts.Add(hrTypeExtraInfo, tb);
            sp.Children.Add(title);
            sp.Children.Add(desc);
            sp.Children.Add(tb);
        }

        var doneButton = new Button {
            Content = "DONE",
            Margin = new Thickness(10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        doneButton.Click += (sender, args) => {
             List<HRTypeExtraInfo> ret = new();
             foreach (var keyValuePair in texts) {
                 keyValuePair.Key.AppliedValue = keyValuePair.Value.Text;
                 ret.Add(keyValuePair.Key);
             }

             onDone.Invoke(ret);
             newWindow.Close();
        };

        grid.Children.Add(doneButton);
        Grid.SetRow(doneButton, 1);

        newWindow.Content = grid;
        newWindow.Show();
    }
}
