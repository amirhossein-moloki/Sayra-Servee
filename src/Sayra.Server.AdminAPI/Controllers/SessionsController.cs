using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("sessions")]
public class SessionsController(ISessionRepository sessionRepository, SessionManager sessionManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? pcId, [FromQuery] int limit = 50)
    {
        var entities = await sessionRepository.GetAllAsync();
        if (!string.IsNullOrEmpty(pcId))
        {
            entities = entities.Where(s => s.PcId == pcId);
        }
        var response = entities.Take(limit).Select(e => new SessionResponse(
            e.SessionId, e.PcId, e.SiteId, e.StartTime, e.EndTime, e.Status, e.Duration, e.CurrentCost, e.RatePerHour));
        return Ok(response);
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(SessionResponse), 201)]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request)
    {
        // Integration with SessionManager would happen here
        var entity = new SessionEntity
        {
            SessionId = Guid.NewGuid().ToString(),
            PcId = request.PcId,
            StartTime = DateTime.UtcNow,
            Status = "ACTIVE",
            SiteId = "default"
        };
        await sessionRepository.AddAsync(entity);
        return CreatedAtAction(nameof(GetAll), new SessionResponse(
            entity.SessionId, entity.PcId, entity.SiteId, entity.StartTime, entity.EndTime, entity.Status, entity.Duration, entity.CurrentCost, entity.RatePerHour));
    }

    [HttpPost("{sessionId}/stop")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Stop(string sessionId)
    {
        // Implementation logic
        return Ok();
    }

    [HttpPost("{sessionId}/pause")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Pause(string sessionId)
    {
        // Implementation logic
        return Ok();
    }

    [HttpPost("{sessionId}/resume")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Resume(string sessionId)
    {
        // Implementation logic
        return Ok();
    }
}
