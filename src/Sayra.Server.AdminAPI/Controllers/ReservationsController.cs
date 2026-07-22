using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ReservationValidationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult Validate([FromQuery] string? username, [FromQuery] string? reservationId)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Username query parameter is required."));
        }

        // Mock reservation validations
        if (username.Equals("amir", StringComparison.OrdinalIgnoreCase) || username.Equals("valid_user", StringComparison.OrdinalIgnoreCase))
        {
            var model = new ReservationModel(
                ReservationId: reservationId ?? "R-101",
                Username: username,
                EndTime: DateTime.UtcNow.AddHours(2),
                RemainingCredits: 30000m
            );
            return Ok(new ReservationValidationResponse(true, model));
        }

        return NotFound(new ErrorResponse("RESERVATION_NOT_FOUND", "Reservation not found or expired"));
    }
}
