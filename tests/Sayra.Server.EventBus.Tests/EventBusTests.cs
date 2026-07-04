using Sayra.Server.EventBus;
using Sayra.Server.EventBus.Events;

namespace Sayra.Server.EventBus.Tests;

public class EventBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldTriggerSubscriber()
    {
        // Arrange
        var bus = new InMemoryEventBus();
        var tcs = new TaskCompletionSource<bool>();

        bus.Subscribe<ClientConnectedEvent>((e, ct) =>
        {
            if (e.ClientId == "test-client")
            {
                tcs.SetResult(true);
            }
            return Task.CompletedTask;
        });

        // Act
        await bus.PublishAsync(new ClientConnectedEvent("test-client", "127.0.0.1"));

        // Assert
        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(result);
    }
}
