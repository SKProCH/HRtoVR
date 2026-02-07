namespace HRtoVRChat;

public interface HRManager {
    bool Init(string d1);
    string GetName();
    int GetHR();
    void Stop();
    bool IsOpen();
    bool IsActive();
}