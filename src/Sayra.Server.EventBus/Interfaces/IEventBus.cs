namespace Sayra.Server.EventBus.Interfaces;

public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class, IEvent;
}

public interface IEventSubscriber
{
    void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class, IEvent;
}
