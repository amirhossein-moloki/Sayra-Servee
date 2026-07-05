using Serilog;
using Serilog.Events;

namespace Sayra.Server.Observability;

public static class LogConfiguration
{
    public static void ConfigureSerilog(string applicationName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File($"logs/{applicationName}-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
