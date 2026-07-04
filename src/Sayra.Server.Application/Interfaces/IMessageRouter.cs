using Sayra.Server.Shared.Messages;

using Sayra.Server.Domain.Entities;

namespace Sayra.Server.Application.Interfaces;

public interface IMessageRouter
{
    Task RouteAsync(string rawMessage);
    Client? GetClient(string clientId);
    void UpdateClient(Client client);
}
