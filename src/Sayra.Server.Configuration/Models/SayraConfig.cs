namespace Sayra.Server.Configuration.Models;

public class SayraConfig
{
    public const string SectionName = "Sayra";

    public HeartbeatConfig Heartbeat { get; set; } = new();
    public SessionConfig Session { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
    public ScalingConfig Scaling { get; set; } = new();
    public BackupConfig Backup { get; set; } = new();
}

public class HeartbeatConfig
{
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 90;
}

public class SessionConfig
{
    public int MaxConcurrentSessionsPerUser { get; set; } = 1;
    public int DefaultSessionDurationMinutes { get; set; } = 60;
}

public class SecurityConfig
{
    public int MaxAuthAttempts { get; set; } = 3;
    public int LockoutDurationMinutes { get; set; } = 15;
    public bool EnforceSignedUpdates { get; set; } = true;
}

public class ScalingConfig
{
    public bool EnableRedis { get; set; } = false;
    public string RedisConnectionString { get; set; } = "localhost:6379";
}

public class BackupConfig
{
    public int BackupIntervalHours { get; set; } = 24;
    public string BackupPath { get; set; } = "./backups";
    public int RetentionDays { get; set; } = 7;
}
