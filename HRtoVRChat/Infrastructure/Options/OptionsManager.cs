using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Infrastructure.Options;

internal class OptionsManager<T> : OptionsMonitor<T>, IOptionsManager<T> where T : class
{
    private readonly IConfiguration _configuration;
    private readonly OptionsConfigPathResolver<T> _configPathResolver;

    public OptionsManager(IOptionsFactory<T> factory, IEnumerable<IOptionsChangeTokenSource<T>> sources, IOptionsMonitorCache<T> cache, 
        IConfiguration configuration, OptionsConfigPathResolver<T> configPathResolver) : base(factory, sources, cache) {
        _configuration = configuration;
        _configPathResolver = configPathResolver;
        factory.Create(Microsoft.Extensions.Options.Options.DefaultName);
    }

    public void Save() {
        _configuration.Set(_configPathResolver.Path, CurrentValue);
    }
}