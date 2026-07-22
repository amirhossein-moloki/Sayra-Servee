using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/sessions")]
public class SessionsController(ISessionRepository sessionRepository, SessionManager sessionManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? pcId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        if (page < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Page number must be greater than or equal to 1."));
        }
        if (limit < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Limit must be greater than or equal to 1."));
        }

        var entities = await sessionRepository.GetAllAsync();
        if (!string.IsNullOrEmpty(pcId))
        {
            entities = entities.Where(s => s.PcId.Equals(pcId, StringComparison.OrdinalIgnoreCase));
        }

        var response = entities
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(e => new SessionResponse(
                e.SessionId, e.PcId, e.SiteId, e.StartTime, e.EndTime, e.Status, e.Duration, e.CurrentCost, e.RatePerHour));

        return Ok(response);
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(SessionResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PcId))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "pcId is a required field."));
        }

        // Conflict check: if the pcId already has an active or paused session
        var allSessions = await sessionRepository.GetAllAsync();
        var hasActive = allSessions.Any(s => s.PcId.Equals(request.PcId, StringComparison.OrdinalIgnoreCase) &&
                                            (s.Status == "ACTIVE" || s.Status == "PAUSED"));
        if (hasActive)
        {
            return Conflict(new ErrorResponse("CONFLICT", $"Target PC '{request.PcId}' already has an active or paused session."));
        }

        var entity = new SessionEntity
        {
            SessionId = Guid.NewGuid().ToString(),
            PcId = request.PcId,
            StartTime = DateTime.UtcNow,
            Status = "ACTIVE",
            SiteId = "default",
            Duration = 60,
            CurrentCost = 0,
            RatePerHour = 15000
        };

        await sessionRepository.AddAsync(entity);
        sessionManager.CreateSession(entity.PcId, entity.SessionId);

        return CreatedAtAction(nameof(GetAll), new SessionResponse(
            entity.SessionId, entity.PcId, entity.SiteId, entity.StartTime, entity.EndTime, entity.Status, entity.Duration, entity.CurrentCost, entity.RatePerHour));
    }

    [HttpPost("{sessionId}/stop")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Stop(string sessionId)
    {
        var e = await sessionRepository.GetByIdAsync(sessionId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Session '{sessionId}' was not found."));
        }

        e.Status = "ENDED";
        e.EndTime = DateTime.UtcNow;
        await sessionRepository.UpdateAsync(e);
        sessionManager.EndSession(e.PcId);

        return Ok();
    }

    [HttpPost("{sessionId}/pause")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Pause(string sessionId)
    {
        var e = await sessionRepository.GetByIdAsync(sessionId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Session '{sessionId}' was not found."));
        }

        e.Status = "PAUSED";
        await sessionRepository.UpdateAsync(e);

        return Ok();
    }

    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Resume(string sessionId)
    {
        var e = await sessionRepository.GetByIdAsync(sessionId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Session '{sessionId}' was not found."));
        }

        e.Status = "ACTIVE";
        await sessionRepository.UpdateAsync(e);

        return Ok();
    }
}
