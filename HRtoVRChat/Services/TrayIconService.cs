using Avalonia;
using Avalonia.Controls;

namespace HRtoVRChat.Services;

public interface ITrayIconService
{
    Window? MainWindow { get; set; }
    Window? ArgumentsWindow { get; set; }
    void Init(Application app);
    void Update(TrayIconManager.UpdateTrayIconInformation info);
}

public class TrayIconService : ITrayIconService
{
    public Window? MainWindow
    {
        get => TrayIconManager.MainWindow;
        set => TrayIconManager.MainWindow = (MainWindow?)value;
    }

    public Window? ArgumentsWindow
    {
        get => TrayIconManager.ArgumentsWindow;
        set => TrayIconManager.ArgumentsWindow = (Arguments?)value;
    }

    public void Init(Application app) => TrayIconManager.Init(app);
    public void Update(TrayIconManager.UpdateTrayIconInformation info) => TrayIconManager.Update(info);
}
