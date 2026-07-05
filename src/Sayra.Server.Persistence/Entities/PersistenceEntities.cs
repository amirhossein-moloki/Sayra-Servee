using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Persistence.Entities;

public class ClientEntity
{
    [Key]
    public string PcId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string IP { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}

public class SessionEntity
{
    [Key]
    public string SessionId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string PcId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Duration { get; set; }

    // Billing fields for crash recovery
    public decimal CurrentCost { get; set; }
    public string? PricePlanId { get; set; }
    public decimal RatePerHour { get; set; }

    public ClientEntity? Client { get; set; }
}

public class CommandAuditEntity
{
    [Key]
    public string CommandId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string PcId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TelemetryEntity
{
    [Key]
    public int Id { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string PcId { get; set; } = string.Empty;
    public float CPU { get; set; }
    public float RAM { get; set; }
    public long Uptime { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AdminUserEntity
{
    [Key]
    public string AdminId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
