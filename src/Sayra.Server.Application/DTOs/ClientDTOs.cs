namespace Sayra.Server.Application.DTOs;

public record ClientResponse(
    string PcId,
    string SiteId,
    string MacAddress,
    string Hostname,
    string Ip,
    string Status,
    DateTime LastSeen
);

public record ClientStateDto(
    string CoreState,
    string SessionStatus,
    string? RemainingTime,
    DateTime? StartTime,
    double? ElapsedSeconds,
    double? TotalDurationMinutes,
    decimal? RatePerHour,
    decimal? CurrentCost,
    string? UserName,
    bool IsKioskLocked
);

public record RegisterClientRequest(
    string MacAddress,
    string Hostname,
    string? SiteId
);
