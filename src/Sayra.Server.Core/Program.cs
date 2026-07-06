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
using Sayra.Server.Configuration;
using Sayra.Server.Configuration.Models;
using Sayra.Server.Deployment;
using Sayra.Server.UpdateSystem.Services;
using Sayra.Server.UpdateSystem.Workflow;
using Sayra.Server.Scaling;
using Sayra.Server.Discovery;
using Sayra.Server.BackupRecovery.Services;
using Sayra.Server.ProductionHardeningFinal.Logging;
using Sayra.Server.Licensing.Services;
using Sayra.Server.Licensing.Models;
using Sayra.Server.Billing.Services;
using Sayra.Server.MultiSite.Interfaces;
using Sayra.Server.FeatureGating.Services;
using Sayra.Server.SecurityLockdown.Services;
using Microsoft.Extensions.Options;
using Serilog;

namespace Sayra.Server.Core;

public class Program
{
    public static void Main(string[] args)
    {
        LogConfiguration.ConfigureSerilog("SayraServer");

        try
        {
            Log.Information("Starting Sayra Server Phase 6 (Enterprise LAN Edition)...");

            // --- Phase 6: Secure Boot & License Validation ---
            var licenseService = new LicenseService(new HardwareFingerprintService());
            var integrityGuard = new IntegrityGuard();

            if (integrityGuard.IsDebuggerAttached())
            {
                Log.Fatal("Debugger detected! Security lockdown active.");
                return;
            }

            if (!licenseService.ValidateLicense("license.lic", out var licenseInfo))
            {
                Log.Fatal("NO VALID LICENSE FOUND. Server cannot start.");
                Log.Information("Hardware Request Code: {Request}", licenseService.GenerateLicenseRequest());
                return;
            }

            Log.Information("License Validated: {Tier} for {IssuedTo} (Expires: {Expiry})",
                licenseInfo?.Tier, licenseInfo?.IssuedTo, licenseInfo?.ExpiryDate);

            CreateHostBuilder(args, licenseInfo!).Build().Run();
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

    public static IHostBuilder CreateHostBuilder(string[] args, LicenseInfo license) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServiceHost()
            .ConfigureServices((hostContext, services) =>
            {
                // Phase 6: Enterprise Modules
                services.AddSingleton(license);
                services.AddSingleton<IHardwareFingerprintService, HardwareFingerprintService>();
                services.AddSingleton<ILicenseService, LicenseService>();
                services.AddSingleton<IBillingEngine, BillingEngine>();
                services.AddSingleton<IInvoiceService, InvoiceService>();
                services.AddScoped<ISiteContext, SiteContext>();
                services.AddSingleton<IFeatureManager>(sp => new FeatureManager(license.Tier));
                services.AddSingleton<IIntegrityGuard, IntegrityGuard>();
                services.AddSingleton<IAuditLogger, SecureAuditLogger>();

                // Phase 5: Configuration
                services.AddSayraConfiguration(hostContext.Configuration);
                // Persistence
                services.AddDbContextFactory<SayraDbContext>(options =>
                    options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));
                services.AddDbContext<SayraDbContext>();

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

                // Phase 5: Update System
                services.AddSingleton<IIntegrityVerifier, IntegrityVerifier>();
                services.AddSingleton<VersionChecker>(sp => new VersionChecker("1.0.0"));
                services.AddSingleton<IUpdateDistributor, UpdateDistributor>();
                services.AddSingleton<UpdateProcessor>(sp => new UpdateProcessor(
                    sp.GetRequiredService<VersionChecker>(),
                    sp.GetRequiredService<IIntegrityVerifier>(),
                    sp.GetRequiredService<IUpdateDistributor>(),
                    "<RSA_PEM_KEY>"));

                // Phase 5: Backup & Recovery
                services.AddSingleton<RestoreManager>();
                services.AddHostedService<DatabaseBackupService>();
                services.AddHostedService<SessionStateSnapshotService>();

                // Phase 5: Final Hardening
                services.AddSingleton<ImmutableAuditLogger>();

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
                var signalRBuilder = services.AddSignalR();

                // Phase 5: Scaling (Redis)
                var sayraConfig = hostContext.Configuration.GetSection(SayraConfig.SectionName).Get<SayraConfig>();
                if (sayraConfig?.Scaling?.EnableRedis == true)
                {
                    signalRBuilder.AddRedisScaling(sayraConfig.Scaling.RedisConnectionString);
                }

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
                services.AddHostedService<DiscoveryService>();
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
        _logger.LogInformation("Sayra Core Engine is running (Phase 5 Active - Production Ready)...");
        await _tcpServer.StartAsync(stoppingToken);
    }
}
