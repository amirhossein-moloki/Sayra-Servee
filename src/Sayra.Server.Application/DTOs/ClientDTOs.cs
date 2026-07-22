using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Application.DTOs;

public record ClientResponse(
    [Required] string PcId,
    [Required] string SiteId,
    [Required] string MacAddress,
    [Required] string Hostname,
    [Required] string Ip,
    [Required] string Status,
    [Required] DateTime LastSeen
);

public record ClientStateDto(
    [Required] string CoreState,
    [Required] string SessionStatus,
    string? RemainingTime,
    DateTime? StartTime,
    double? ElapsedSeconds,
    double? TotalDurationMinutes,
    decimal? RatePerHour,
    decimal? CurrentCost,
    string? UserName,
    [Required] bool IsKioskLocked
);

public record RegisterClientRequest(
    [Required(ErrorMessage = "macAddress is required")] string MacAddress,
    [Required(ErrorMessage = "hostname is required")] string Hostname,
    string? SiteId
);
