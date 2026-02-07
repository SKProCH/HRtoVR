using System.Threading.Tasks;

namespace HRtoVRChat.Services;

public interface IHRService
{
    Task StartAsync();
    void Stop(bool quitApp = false, bool autoStart = false);
    void RestartHRListener();
    Task StartHRListenerAsync(bool fromRestart = false);
    void StopHRListener();
}
