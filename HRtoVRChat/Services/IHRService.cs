namespace HRtoVRChat.Services;

public interface IHRService
{
    void Start();
    void Stop(bool quitApp = false, bool autoStart = false);
    void RestartHRListener();
    void StartHRListener(bool fromRestart = false);
    void StopHRListener();
    void HandleCommand(string? input);
}
