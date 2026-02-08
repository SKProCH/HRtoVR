using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Infrastructure.Options;

public class AutoConfigurationChangeTokenSource<TOptions> : ConfigurationChangeTokenSource<TOptions>
{
    public AutoConfigurationChangeTokenSource(IConfiguration configuration)
        // Options.DefaultName — это пустая строка, стандартное имя для неименованных опций
        : base(Microsoft.Extensions.Options.Options.DefaultName, configuration.GetSection(typeof(TOptions).Name))
    {
    }
}