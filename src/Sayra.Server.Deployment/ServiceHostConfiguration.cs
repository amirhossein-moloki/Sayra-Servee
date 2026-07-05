using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sayra.Server.Deployment;

public static class ServiceHostConfiguration
{
    public static IHostBuilder ConfigureServiceHost(this IHostBuilder hostBuilder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return hostBuilder.UseWindowsService(options =>
            {
                options.ServiceName = "SayraServer";
            });
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return hostBuilder.UseSystemd();
        }

        return hostBuilder;
    }
}
