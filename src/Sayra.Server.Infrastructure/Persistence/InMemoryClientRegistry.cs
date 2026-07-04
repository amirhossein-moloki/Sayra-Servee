using System.Collections.Concurrent;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Domain.Entities;

namespace Sayra.Server.Infrastructure.Persistence;

public class InMemoryClientRegistry : IClientRegistry
{
    private readonly ConcurrentDictionary<string, Client> _clients = new();

    public void AddOrUpdate(Client client)
    {
        _clients.AddOrUpdate(client.Id, client, (_, existing) => client);
    }

    public Client? GetById(string clientId)
    {
        return _clients.TryGetValue(clientId, out var client) ? client : null;
    }

    public IEnumerable<Client> GetAll()
    {
        return _clients.Values;
    }

    public void Remove(string clientId)
    {
        _clients.TryRemove(clientId, out _);
    }
}
