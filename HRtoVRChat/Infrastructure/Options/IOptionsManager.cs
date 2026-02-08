using Microsoft.Extensions.Options;

namespace HRtoVRChat.Infrastructure.Options;

public interface IOptionsManager<T> : IOptionsMonitor<T> where T : class {
    void Save();
}