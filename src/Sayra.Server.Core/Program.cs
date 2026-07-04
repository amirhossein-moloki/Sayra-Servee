using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Application.Messaging;
using Sayra.Server.Infrastructure.Persistence;
using Sayra.Server.Infrastructure.BackgroundServices;
using Sayra.Server.Network.Tcp;
using Sayra.Server.Security;
using Sayra.Server.Authentication;
using Sayra.Server.Session;
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

                // Options
                services.Configure<SecurityOptions>(hostContext.Configuration.GetSection("Security"));

                // Security
                services.AddSingleton<IEncryptionService, EncryptionService>();
                services.AddSingleton<ISignatureService, SignatureService>();
                services.AddSingleton<IReplayProtectionService, ReplayProtectionService>();
                services.AddSingleton<ISecureMessageValidator, SecureMessageValidator>();

                // Authentication
                services.AddSingleton<IChallengeGenerator, ChallengeGenerator>();
                services.AddSingleton<IAuthSessionManager, AuthSessionManager>();
                services.AddSingleton<IAuthService, AuthService>();

                // Session
                services.AddSingleton<ISessionRegistry, SessionRegistry>();
                services.AddSingleton<ISessionManager, SessionManager>();

                // Application
                services.AddSingleton<CommandAuthorizer>();
                services.AddSingleton<ISecureMessageDispatcher, SecureMessageDispatcher>();
                services.AddSingleton<IMessageRouter, MessageRouter>();

                // Network
                services.AddSingleton<TcpServer>(sp =>
                    new TcpServer(
                        sp.GetRequiredService<ILogger<TcpServer>>(),
                        sp.GetRequiredService<IMessageRouter>(),
                        sp.GetRequiredService<IAuthService>(),
                        sp.GetRequiredService<ISecureMessageValidator>(),
                        sp.GetRequiredService<ISignatureService>(),
                        sp.GetRequiredService<IEncryptionService>(),
                        sp.GetRequiredService<ISessionManager>(),
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
