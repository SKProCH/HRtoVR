using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HRtoVRChat.Services;

public interface IBrowserService
{
    void OpenUrl(string url);
}

public class BrowserService : IBrowserService
{
    public void OpenUrl(string url)
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
}
