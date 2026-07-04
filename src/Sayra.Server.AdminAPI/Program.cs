using Sayra.Server.Application.Interfaces;
using Sayra.Server.Infrastructure.Persistence.Repositories;
using Sayra.Server.Persistence;
using Sayra.Server.Monitoring.Interfaces;
using Sayra.Server.Monitoring.Services;
using Sayra.Server.Realtime.Hubs;
using Sayra.Server.ProductionHardening.Middleware;
using Sayra.Server.ProductionHardening.CircuitBreaker;
using Sayra.Server.Observability;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Phase 4: Observability
LogConfiguration.ConfigureSerilog("AdminAPI");
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

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

app.MapControllers();
app.MapHub<AdminHub>("/hubs/admin");

app.Run();
