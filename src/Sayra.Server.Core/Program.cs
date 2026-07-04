using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Application.Messaging;
using Sayra.Server.Infrastructure.Persistence;
using Sayra.Server.Infrastructure.BackgroundServices;
using Sayra.Server.Network.Tcp;
using Serilog;

namespace Sayra.Server.Core;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting Sayra Server...");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Server terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                // Infrastructure
                services.AddSingleton<IClientRegistry, InMemoryClientRegistry>();

                // Application
                services.AddSingleton<IMessageRouter, MessageRouter>();

                // Network
                services.AddSingleton<TcpServer>(sp =>
                    new TcpServer(
                        sp.GetRequiredService<ILogger<TcpServer>>(),
                        sp.GetRequiredService<IMessageRouter>(),
                        5000));

                // Hosted Services
                services.AddHostedService<ServerWorker>();
                services.AddHostedService<HeartbeatMonitorService>();
            });
}

public class ServerWorker : BackgroundService
{
    private readonly TcpServer _tcpServer;
    private readonly ILogger<ServerWorker> _logger;

    public ServerWorker(TcpServer tcpServer, ILogger<ServerWorker> logger)
    {
        _tcpServer = tcpServer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sayra Core Engine is running...");
        await _tcpServer.StartAsync(stoppingToken);
    }
}
