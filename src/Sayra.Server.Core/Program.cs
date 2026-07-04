using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Application.Messaging;
using Sayra.Server.Application.EventHandlers;
using Sayra.Server.EventBus;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Infrastructure.Persistence;
using Sayra.Server.Infrastructure.Persistence.Repositories;
using Sayra.Server.Infrastructure.BackgroundServices;
using Sayra.Server.Persistence;
using Sayra.Server.Network.Tcp;
using Microsoft.EntityFrameworkCore;
using Sayra.Server.Security;
using Sayra.Server.Authentication;
using Sayra.Server.Session;
using Sayra.Server.Observability;
using Sayra.Server.Monitoring;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Services;
using Sayra.Server.Realtime;
using Sayra.Server.Realtime.Hubs;
using Sayra.Server.ProductionHardening.CircuitBreaker;
using Serilog;

namespace Sayra.Server.Core;

public class Program
{
    public static void Main(string[] args)
    {
        LogConfiguration.ConfigureSerilog("SayraServer");

        try
        {
            Log.Information("Starting Sayra Server Phase 4...");
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
                // Persistence
                services.AddDbContext<SayraDbContext>(options =>
                    options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));

                // Event Bus
                var eventBus = new InMemoryEventBus();
                services.AddSingleton<IEventPublisher>(eventBus);
                services.AddSingleton<IEventSubscriber>(eventBus);

                // Infrastructure
                services.AddSingleton<IClientRegistry, InMemoryClientRegistry>();

                // Repositories
                services.AddScoped<IClientRepository, ClientRepository>();
                services.AddScoped<IAdminUserRepository, AdminUserRepository>();
                services.AddScoped<ITelemetryRepository, TelemetryRepository>();

                // Phase 4: Decorated Repositories (Hardening)
                services.AddSingleton<DbCircuitBreaker>();
                services.AddScoped<SessionRepository>(); // Raw implementation
                services.AddScoped<ISessionRepository, SessionRepositoryDecorator>(sp =>
                    new SessionRepositoryDecorator(sp.GetRequiredService<SessionRepository>(), sp.GetRequiredService<DbCircuitBreaker>()));

                services.AddScoped<CommandRepository>(); // Raw implementation
                services.AddScoped<ICommandRepository, CommandRepositoryDecorator>(sp =>
                    new CommandRepositoryDecorator(sp.GetRequiredService<CommandRepository>(), sp.GetRequiredService<DbCircuitBreaker>()));

                // Event Handlers
                services.AddSingleton<PersistenceEventHandlers>();

                // Phase 4: Monitoring & Realtime
                services.AddSingleton<IMetricsService, MetricsAggregator>();
                services.AddSingleton<IAlertService, AlertService>();
                services.AddSingleton<MonitoringEventHandler>();
                services.AddSingleton<RealtimeEventHandler>();

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

                // Initialize Event Handlers
                services.AddHostedService<EventHandlerInitializer>();

                // SignalR
                services.AddSignalR();

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
    private readonly MonitoringEventHandler _monitoringHandler;
    private readonly RealtimeEventHandler _realtimeHandler;

    public ServerWorker(
        TcpServer tcpServer,
        ILogger<ServerWorker> logger,
        MonitoringEventHandler monitoringHandler,
        RealtimeEventHandler realtimeHandler)
    {
        _tcpServer = tcpServer;
        _logger = logger;
        _monitoringHandler = monitoringHandler;
        _realtimeHandler = realtimeHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sayra Core Engine is running (Phase 4 Active)...");
        await _tcpServer.StartAsync(stoppingToken);
    }
}
