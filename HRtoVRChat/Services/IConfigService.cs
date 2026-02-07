using HRtoVRChat;

namespace HRtoVRChat.Services;

public interface IConfigService
{
    Config LoadedConfig { get; }
    UIConfig LoadedUIConfig { get; }
    void CreateConfig();
    void SaveConfig(Config config);
    void SaveConfig(UIConfig uiConfig);
    bool DoesConfigExist();
}
