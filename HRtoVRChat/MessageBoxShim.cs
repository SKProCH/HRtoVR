using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace HRtoVRChat;

// Replicating the namespaces expected by the existing code
public static class MessageBoxManager
{
    public static HRtoVRChat.IMsBoxWindow<ButtonResult> GetMessageBoxStandardWindow(MessageBoxStandardParams parameters)
    {
        return new HRtoVRChat.MsBoxWindow(parameters);
    }
}

public class MessageBoxStandardParams
{
    public ButtonEnum ButtonDefinitions { get; set; } = ButtonEnum.Ok;
    public string ContentTitle { get; set; } = "Title";
    public string ContentHeader { get; set; } = "";
    public string ContentMessage { get; set; } = "Message";
    public WindowIcon? WindowIcon { get; set; }
    public Icon Icon { get; set; } = Icon.None;
    public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterScreen;
}

public enum ButtonEnum
{
    Ok,
    YesNo,
    OkCancel
}

public enum Icon
{
    None,
    Error,
    Info,
    Warning,
    Question
}

[Flags]
public enum ButtonResult
{
    Ok = 1,
    Yes = 6,
    No = 7,
    Cancel = 2,
    None = 0
}

public interface IMsBoxWindow<T>
{
    Task<T> Show();
    Task<T> ShowDialog(Window owner);
}

public class MsBoxWindow : Window, IMsBoxWindow<ButtonResult>
{
    private readonly MessageBoxStandardParams _params;

    public MsBoxWindow(MessageBoxStandardParams parameters)
    {
        _params = parameters;
        this.SizeToContent = SizeToContent.WidthAndHeight;
        this.WindowStartupLocation = parameters.WindowStartupLocation;
        this.Title = parameters.ContentTitle;
        this.Icon = parameters.WindowIcon;
        this.CanResize = false;

        // Simple style
        this.SystemDecorations = SystemDecorations.Full;
        this.Width = 450;
        this.Height = 250;

        // Layout
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(20)
        };

        var contentStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Header
        if (!string.IsNullOrEmpty(parameters.ContentHeader))
        {
            var headerBlock = new TextBlock
            {
                Text = parameters.ContentHeader,
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            contentStack.Children.Add(headerBlock);
        }

        var textBlock = new TextBlock
        {
            Text = parameters.ContentMessage,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        contentStack.Children.Add(textBlock);

        Grid.SetRow(contentStack, 0);
        grid.Children.Add(contentStack);

        var buttonStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        if (_params.ButtonDefinitions == ButtonEnum.YesNo)
        {
            var btnYes = new Button { Content = "Yes", HorizontalContentAlignment = HorizontalAlignment.Center, Width = 70 };
            btnYes.Click += (_, _) => { Close(ButtonResult.Yes); };
            var btnNo = new Button { Content = "No", HorizontalContentAlignment = HorizontalAlignment.Center, Width = 70 };
            btnNo.Click += (_, _) => { Close(ButtonResult.No); };
            buttonStack.Children.Add(btnYes);
            buttonStack.Children.Add(btnNo);
        }
        else // Ok or default
        {
            var btnOk = new Button { Content = "OK", HorizontalContentAlignment = HorizontalAlignment.Center, Width = 70 };
            btnOk.Click += (_, _) => { Close(ButtonResult.Ok); };
            buttonStack.Children.Add(btnOk);
        }

        Grid.SetRow(buttonStack, 1);
        grid.Children.Add(buttonStack);

        this.Content = grid;
    }

    public new Task<ButtonResult> Show()
    {
        var tcs = new TaskCompletionSource<ButtonResult>();
        this.Closed += (_, _) => {
            if (!tcs.Task.IsCompleted) tcs.TrySetResult(_result);
        };
        base.Show();
        return tcs.Task;
    }

    public new async Task<ButtonResult> ShowDialog(Window owner)
    {
        await base.ShowDialog(owner);
        return _result;
    }

    private ButtonResult _result = ButtonResult.None;

    private void Close(ButtonResult result)
    {
        _result = result;
        base.Close(result);
    }
}