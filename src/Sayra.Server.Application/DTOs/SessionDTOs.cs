namespace Sayra.Server.Application.DTOs;

public record SessionResponse(
    string SessionId,
    string PcId,
    string SiteId,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    double Duration,
    decimal CurrentCost,
    decimal RatePerHour
);

public record StartSessionRequest(
    string PcId,
    string? PricePlanId,
    string? UserId
);
