using Microsoft.Extensions.DependencyInjection;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Persistence.Entities;

namespace Sayra.Server.Application.EventHandlers;

public class PersistenceEventHandlers
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PersistenceEventHandlers(
        IEventSubscriber subscriber,
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        subscriber.Subscribe<ClientConnectedEvent>(HandleClientConnectedAsync);
        subscriber.Subscribe<ClientAuthenticatedEvent>(HandleClientAuthenticatedAsync);
        subscriber.Subscribe<SessionStartedEvent>(HandleSessionStartedAsync);
        subscriber.Subscribe<SessionEndedEvent>(HandleSessionEndedAsync);
        subscriber.Subscribe<CommandExecutedEvent>(HandleCommandExecutedAsync);
        subscriber.Subscribe<TelemetryReceivedEvent>(HandleTelemetryReceivedAsync);
    }

    private async Task HandleClientConnectedAsync(ClientConnectedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();

        await clientRepository.UpsertAsync(new ClientEntity
        {
            PcId = @event.ClientId,
            IP = @event.IpAddress,
            Status = "Online",
            LastSeen = DateTime.UtcNow
        });
    }

    private async Task HandleClientAuthenticatedAsync(ClientAuthenticatedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();

        var client = await clientRepository.GetByPcIdAsync(@event.PcId);
        if (client != null)
        {
            client.MacAddress = @event.MacAddress;
            client.LastSeen = DateTime.UtcNow;
            await clientRepository.UpsertAsync(client);
        }
    }

    private async Task HandleSessionStartedAsync(SessionStartedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        await sessionRepository.AddAsync(new SessionEntity
        {
            SessionId = @event.SessionId,
            PcId = @event.PcId,
            StartTime = @event.OccurredAt,
            Status = "Active"
        });
    }

    private async Task HandleSessionEndedAsync(SessionEndedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        var session = await sessionRepository.GetByIdAsync(@event.SessionId);
        if (session != null)
        {
            session.EndTime = @event.EndTime;
            session.Status = "Ended";
            session.Duration = (@event.EndTime - session.StartTime).TotalMinutes;
            await sessionRepository.UpdateAsync(session);
        }
    }

    private async Task HandleCommandExecutedAsync(CommandExecutedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var commandRepository = scope.ServiceProvider.GetRequiredService<ICommandRepository>();

        await commandRepository.AddAsync(new CommandAuditEntity
        {
            CommandId = @event.CommandId,
            PcId = @event.PcId,
            Action = @event.Action,
            Result = @event.Result,
            Timestamp = @event.OccurredAt
        });
    }

    private async Task HandleTelemetryReceivedAsync(TelemetryReceivedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var telemetryRepository = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();

        await telemetryRepository.AddAsync(new TelemetryEntity
        {
            PcId = @event.PcId,
            CPU = @event.CpuUsage,
            RAM = @event.RamUsage,
            Uptime = @event.Uptime,
            Timestamp = @event.OccurredAt
        });
    }
}
