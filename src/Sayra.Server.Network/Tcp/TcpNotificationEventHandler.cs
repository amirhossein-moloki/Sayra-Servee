using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Network.Tcp;

public class TcpNotificationEventHandler
{
    private readonly ITcpConnectionRegistry _connectionRegistry;
    private readonly ILogger<TcpNotificationEventHandler> _logger;

    public TcpNotificationEventHandler(IEventSubscriber subscriber, ITcpConnectionRegistry connectionRegistry, ILogger<TcpNotificationEventHandler> logger)
    {
        _connectionRegistry = connectionRegistry;
        _logger = logger;

        subscriber.Subscribe<SecurityBreachDetectedEvent>(HandleSecurityBreach);
        subscriber.Subscribe<BillingUpdateEvent>(HandleBillingUpdate);
    }

    private async Task HandleSecurityBreach(SecurityBreachDetectedEvent @event, CancellationToken ct)
    {
        _logger.LogWarning("Handling Security Breach Detected event for client: {ClientId}", @event.ClientId);
        var connection = _connectionRegistry.GetConnection(@event.ClientId);
        if (connection != null)
        {
            var msg = new SecurityBreachDetectedMessage
            {
                Type = "SECURITY_BREACH_DETECTED",
                ClientId = @event.ClientId,
                Severity = @event.Severity,
                Description = @event.Description,
                Details = @event.Details
            };
            await connection.SendMessageAsync(msg);
        }
    }

    private async Task HandleBillingUpdate(BillingUpdateEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("Handling Billing Update event for client: {ClientId}", @event.ClientId);
        var connection = _connectionRegistry.GetConnection(@event.ClientId);
        if (connection != null)
        {
            var msg = new BillingUpdateMessage
            {
                Type = "BILLING_UPDATE",
                ClientId = @event.ClientId,
                SessionId = @event.SessionId,
                RatePerHour = @event.RatePerHour,
                RemainingCredits = @event.RemainingCredits
            };
            await connection.SendMessageAsync(msg);
        }
    }
}
