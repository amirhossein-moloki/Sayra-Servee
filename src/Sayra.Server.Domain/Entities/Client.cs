using Sayra.Server.Domain.Enums;

namespace Sayra.Server.Domain.Entities;

public class Client
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public ClientStatus Status { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public string OSVersion { get; set; } = string.Empty;
}
