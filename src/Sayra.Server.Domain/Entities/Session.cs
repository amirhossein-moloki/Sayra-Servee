namespace Sayra.Server.Domain.Entities;

public class Session
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string UserId { get; set; } = string.Empty;
}
