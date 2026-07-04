using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Interfaces;

public interface IMessageRouter
{
    Task RouteAsync(string rawMessage);
}
