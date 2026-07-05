using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sayra.Server.Configuration.Models;

namespace Sayra.Server.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddSayraConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var sayraSection = configuration.GetSection(SayraConfig.SectionName);
        services.Configure<SayraConfig>(sayraSection);

        // Explicitly register sub-configs for easier injection if needed
        services.Configure<HeartbeatConfig>(sayraSection.GetSection("Heartbeat"));
        services.Configure<SessionConfig>(sayraSection.GetSection("Session"));
        services.Configure<SecurityConfig>(sayraSection.GetSection("Security"));
        services.Configure<ScalingConfig>(sayraSection.GetSection("Scaling"));
        services.Configure<BackupConfig>(sayraSection.GetSection("Backup"));

        return services;
    }
}
