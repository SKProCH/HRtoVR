using Microsoft.Extensions.Options;

namespace HRtoVR.Infrastructure.Options;

public interface IOptionsManager<T> : IOptionsMonitor<T> where T : class {
    void Save();
}