using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HRtoVRChat.Infrastructure.Options;

public static class OptionsExtensions {
    public static IServiceCollection ConfigureOptionsPath<TOptions>(this IServiceCollection services, string path)
        where TOptions : class {
        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(provider =>
            new ConfigurationChangeTokenSource<TOptions>(Microsoft.Extensions.Options.Options.DefaultName,
                provider.GetRequiredService<IConfiguration>().GetSection(path)));
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
            new NamedConfigureFromConfigurationOptions<TOptions>(Microsoft.Extensions.Options.Options.DefaultName,
                provider.GetRequiredService<IConfiguration>().GetSection(path), _ => { }));
        services.AddSingleton(new OptionsConfigPathResolver<TOptions>(path));
        return services;
    }
}