using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Application.DTOs;

public record ReservationModel(
    [Required] string ReservationId,
    [Required] string Username,
    [Required] DateTime EndTime,
    [Required] decimal RemainingCredits
);

public record ReservationValidationResponse(
    [Required] bool Success,
    [Required] ReservationModel Reservation
);
