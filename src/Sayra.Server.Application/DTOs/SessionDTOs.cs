using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Application.DTOs;

public record SessionResponse(
    [Required] string SessionId,
    [Required] string PcId,
    [Required] string SiteId,
    [Required] DateTime StartTime,
    DateTime? EndTime,
    [Required] string Status,
    [Required] double Duration,
    [Required] decimal CurrentCost,
    [Required] decimal RatePerHour
);

public record StartSessionRequest(
    [Required(ErrorMessage = "pcId is required")] string PcId,
    string? PricePlanId,
    string? UserId
);
