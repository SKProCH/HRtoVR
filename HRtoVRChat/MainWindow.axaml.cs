using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HRtoVRChat.ViewModels;
using HRtoVRChat_OSC_SDK;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace HRtoVRChat;

public partial class MainWindow : Window {

    public MainWindow() {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        var vm = new MainWindowViewModel();
        DataContext = vm;

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
}
