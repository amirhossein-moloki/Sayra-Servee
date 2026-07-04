using System.Collections.Concurrent;
using System.Threading.Channels;
using Sayra.Server.EventBus.Interfaces;

namespace Sayra.Server.EventBus;

public class InMemoryEventBus : IEventPublisher, IEventSubscriber
{
    private readonly Channel<IEvent> _channel;
    private readonly ConcurrentDictionary<Type, List<Func<IEvent, CancellationToken, Task>>> _handlers = new();

    public InMemoryEventBus()
    {
        _channel = Channel.CreateUnbounded<IEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _ = ProcessEventsAsync();
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class, IEvent
    {
        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class, IEvent
    {
        var eventType = typeof(T);
        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<IEvent, CancellationToken, Task>>());

        lock (handlers)
        {
            handlers.Add((e, ct) => handler((T)e, ct));
        }
    }

    private async Task ProcessEventsAsync()
    {
        await foreach (var @event in _channel.Reader.ReadAllAsync())
        {
            var eventType = @event.GetType();
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                List<Func<IEvent, CancellationToken, Task>> handlersCopy;
                lock (handlers)
                {
                    handlersCopy = handlers.ToList();
                }

                // Execute all handlers concurrently to avoid blocking the event loop
                var tasks = handlersCopy.Select(async handler =>
                {
                    try
                    {
                        await handler(@event, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        // Log exception (In a real system we would use ILogger)
                        Console.WriteLine($"Error handling event {eventType.Name}: {ex.Message}");
                    }
                });

                // We don't await Task.WhenAll(tasks) here to avoid blocking the processing of the next event in the channel.
                // However, we want to ensure events are processed.
                // Given the requirement "No blocking handlers" and "Async event dispatching",
                // we should at least start them.
                _ = Task.WhenAll(tasks);
            }
        }
    }
}
