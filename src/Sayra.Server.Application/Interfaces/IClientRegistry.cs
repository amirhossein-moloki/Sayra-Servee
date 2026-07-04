using Sayra.Server.Domain.Entities;

namespace Sayra.Server.Application.Interfaces;

public interface IClientRegistry
{
    void AddOrUpdate(Client client);
    Client? GetById(string clientId);
    IEnumerable<Client> GetAll();
    void Remove(string clientId);
}
