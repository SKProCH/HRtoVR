using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HRtoVR.Infrastructure.Options;

public class AutoConfigureFromConfigurationOptions<TOptions> : ConfigureFromConfigurationOptions<TOptions>
    where TOptions : class {
    public AutoConfigureFromConfigurationOptions(IConfiguration configuration)
        // Передаем секцию, имя которой совпадает с именем класса TOptions
        : base(configuration.GetSection(typeof(TOptions).Name)) {
    }
}