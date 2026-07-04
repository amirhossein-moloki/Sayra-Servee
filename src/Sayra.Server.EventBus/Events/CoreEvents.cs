using Sayra.Server.EventBus.Interfaces;

namespace Sayra.Server.EventBus.Events;

public abstract record BaseEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record ClientConnectedEvent(string ClientId, string IpAddress) : BaseEvent;

public record ClientAuthenticatedEvent(string ClientId, string PcId, string MacAddress) : BaseEvent;

public record SessionStartedEvent(string SessionId, string PcId) : BaseEvent;

public record SessionEndedEvent(string SessionId, string PcId, DateTime EndTime) : BaseEvent;

public record CommandExecutedEvent(string CommandId, string PcId, string Action, string Result) : BaseEvent;

public record TelemetryReceivedEvent(string PcId, float CpuUsage, float RamUsage, long Uptime) : BaseEvent;
