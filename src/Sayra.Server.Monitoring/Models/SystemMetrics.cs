namespace Sayra.Server.Monitoring.Models;

public record SystemMetrics
{
    public int ActiveClients { get; init; }
    public int ActiveSessions { get; init; }
    public double AverageCpuUsage { get; init; }
    public double AverageRamUsage { get; init; }
    public int CommandsExecuted { get; init; }
    public int FailedCommands { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

public record TelemetryDataPoint(DateTime Timestamp, float Value);
