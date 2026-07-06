namespace Sayra.Server.Application.DTOs;

public record TelemetryResponse(
    float Cpu,
    float Ram,
    long Uptime,
    DateTime Timestamp
);

public record SystemHealthResponse(
    string Status,
    string Version,
    int Uptime,
    bool DbConnected,
    bool RedisConnected
);

public record StatusSnapshotResponse(
    DateTime Timestamp,
    IEnumerable<ClientResponse> Clients,
    IEnumerable<SessionResponse> ActiveSessions
);
