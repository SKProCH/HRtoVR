using Microsoft.Extensions.Logging;

namespace HRtoVRChat;

public interface IHrListener {
    bool Init(string d1);
    string Name { get; }
    int GetHR();
    void Stop();
    bool IsOpen();
    bool IsActive();
}
