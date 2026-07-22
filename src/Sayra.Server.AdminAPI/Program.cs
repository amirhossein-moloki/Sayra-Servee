using Sayra.Server.Application.Interfaces;
using Sayra.Server.Infrastructure.Persistence.Repositories;
using Sayra.Server.Persistence;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Services;
using Sayra.Server.Realtime.Hubs;
using Sayra.Server.ProductionHardening.Middleware;
using Sayra.Server.ProductionHardening.CircuitBreaker;
using Sayra.Server.Observability;
using Sayra.Server.Authentication;
using Sayra.Server.Billing.Services;
using Sayra.Server.Licensing.Services;
using Sayra.Server.Configuration.Models;
using Sayra.Server.UpdateSystem.Services;
using Sayra.Server.Session;
using Sayra.Server.Security;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.EventBus;
using Sayra.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Sayra.Server.AdminAPI.Authentication;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Phase 4: Observability
LogConfiguration.ConfigureSerilog("AdminAPI");
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            var errorMsg = "Validation failed: " + string.Join("; ", errors);
            var details = string.Join(", ", errors);
            return new BadRequestObjectResult(new ErrorResponse("BAD_REQUEST", errorMsg, details));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Phase 3: Bearer Authentication
builder.Services.AddAuthentication("Bearer")
    .AddScheme<BearerAuthOptions, BearerAuthHandler>("Bearer", null);
builder.Services.AddAuthorization();

// Database configuration
builder.Services.AddDbContext<SayraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<ITelemetryRepository, TelemetryRepository>();

// Phase 4: Decorated Repositories (Hardening)
builder.Services.AddSingleton<DbCircuitBreaker>();
builder.Services.AddScoped<SessionRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepositoryDecorator>(sp =>
    new SessionRepositoryDecorator(sp.GetRequiredService<SessionRepository>(), sp.GetRequiredService<DbCircuitBreaker>()));

builder.Services.AddScoped<CommandRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepositoryDecorator>(sp =>
    new CommandRepositoryDecorator(sp.GetRequiredService<CommandRepository>(), sp.GetRequiredService<DbCircuitBreaker>()));

// Monitoring (Shared Singletons for the Dashboard)
// NOTE: In Phase 5, these should be backed by a distributed store (Redis) if Core and API are separate processes.
builder.Services.AddSingleton<IMetricsService, MetricsAggregator>();
builder.Services.AddSingleton<IAlertService, AlertService>();

// Module Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChallengeGenerator, ChallengeGenerator>();
builder.Services.AddSingleton<IAuthSessionManager, AuthSessionManager>();
builder.Services.AddSingleton<ISignatureService, SignatureService>();
builder.Services.AddSingleton<IEventPublisher, InMemoryEventBus>();
builder.Services.AddScoped<BillingEngine>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddScoped<UpdateDistributor>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<IClientRegistry, InMemoryClientRegistry>();
builder.Services.AddSingleton<ISessionRegistry, SessionRegistry>();
builder.Services.AddSingleton<IHardwareFingerprintService, HardwareFingerprintService>();

// Configuration
builder.Services.Configure<SayraConfig>(builder.Configuration.GetSection("SayraConfig"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Phase 4: Production Hardening
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AdminHub>("/hubs/admin");

app.Run();
