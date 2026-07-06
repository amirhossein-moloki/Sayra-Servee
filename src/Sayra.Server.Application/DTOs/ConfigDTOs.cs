namespace Sayra.Server.Application.DTOs;

public record SayraConfigResponse(
    HeartbeatConfig Heartbeat,
    SessionConfig Session,
    SecurityConfig Security,
    ScalingConfig Scaling,
    BackupConfig Backup
);

public record HeartbeatConfig(int IntervalSeconds, int TimeoutSeconds);
public record SessionConfig(int MaxConcurrentSessionsPerUser, int DefaultSessionDurationMinutes);
public record SecurityConfig(int MaxAuthAttempts, int LockoutDurationMinutes, bool EnforceSignedUpdates);
public record ScalingConfig(bool EnableRedis, string? RedisConnectionString);
public record BackupConfig(int BackupIntervalHours, string BackupPath, int RetentionDays);
