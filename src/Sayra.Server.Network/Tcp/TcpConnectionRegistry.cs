using System.Collections.Concurrent;

namespace Sayra.Server.Network.Tcp;

public interface ITcpConnectionRegistry
{
    void Register(string clientId, ClientConnection connection);
    void Unregister(string clientId);
    ClientConnection? GetConnection(string clientId);
    IEnumerable<string> GetConnectedClientIds();
}

public class TcpConnectionRegistry : ITcpConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();

    public void Register(string clientId, ClientConnection connection)
    {
        if (_connections.TryGetValue(clientId, out var oldConnection))
        {
            try
            {
                oldConnection.Disconnect();
            }
            catch { }
        }
        _connections[clientId] = connection;
    }

    public void Unregister(string clientId)
    {
        _connections.TryRemove(clientId, out _);
    }

    public ClientConnection? GetConnection(string clientId)
    {
        _connections.TryGetValue(clientId, out var connection);
        return connection;
    }

    public IEnumerable<string> GetConnectedClientIds() => _connections.Keys;
}
