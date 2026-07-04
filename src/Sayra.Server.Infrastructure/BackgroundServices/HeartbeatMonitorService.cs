using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Domain.Enums;

namespace Sayra.Server.Infrastructure.BackgroundServices;

public class HeartbeatMonitorService : BackgroundService
{
    private readonly IClientRegistry _clientRegistry;
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public HeartbeatMonitorService(IClientRegistry clientRegistry, ILogger<HeartbeatMonitorService> logger)
    {
        _clientRegistry = clientRegistry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var clients = _clientRegistry.GetAll().ToList();

            foreach (var client in clients)
            {
                if (client.Status != ClientStatus.Offline && (now - client.LastHeartbeat) > _timeout)
                {
                    _logger.LogWarning("Client {ClientId} timed out. Last heartbeat: {LastHeartbeat}", client.Id, client.LastHeartbeat);
                    client.Status = ClientStatus.Offline;
                    _clientRegistry.AddOrUpdate(client);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
