using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sayra.Server.Application.EventHandlers;

namespace Sayra.Server.Core;

public class EventHandlerInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public EventHandlerInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve PersistenceEventHandlers to trigger its constructor and subscription logic
        _serviceProvider.GetService<PersistenceEventHandlers>();
        _serviceProvider.GetService<Sayra.Server.Network.Tcp.TcpNotificationEventHandler>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
