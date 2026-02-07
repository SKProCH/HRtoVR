using Microsoft.Extensions.Logging;

namespace HRtoVRChat;

public interface IHrListener {
    void Start();
    string Name { get; }
    int GetHR();
    void Stop();
    bool IsOpen();
    bool IsActive();
}
