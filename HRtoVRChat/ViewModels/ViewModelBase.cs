using ReactiveUI;

namespace HRtoVRChat.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public static void OpenUrl(string url)
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") {
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            });
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
            System.Diagnostics.Process.Start("xdg-open", url);
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
            System.Diagnostics.Process.Start("open", url);
        }
        else {
            try {
                // Fallback
                if (url.Contains("github"))
                    System.Diagnostics.Process.Start("https://github.com/200Tigersbloxed/HRtoVRChat_OSC");
            }
            catch (System.Exception) { }
        }
    }
}