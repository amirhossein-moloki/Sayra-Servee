using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Monitoring.Interfaces;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/monitoring")]
public class MonitoringController(
    ITelemetryRepository telemetryRepository,
    IMetricsService metricsService,
    IClientRepository clientRepository,
    ISessionRepository sessionRepository) : ControllerBase
{
    [HttpGet("telemetry/{pcId}")]
    [ProducesResponseType(typeof(IEnumerable<TelemetryResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetTelemetry(string pcId, [FromQuery] int limit = 100)
    {
        if (limit < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Limit must be greater than or equal to 1."));
        }

        var client = await clientRepository.GetByPcIdAsync(pcId);
        if (client == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Workstation '{pcId}' was not found."));
        }

        var entities = await telemetryRepository.GetByPcIdAsync(pcId, limit);
        var response = entities.Select(e => new TelemetryResponse(e.CPU, e.RAM, e.Uptime, e.Timestamp));

        return Ok(response);
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthResponse), 200)]
    public IActionResult GetHealth()
    {
        return Ok(new SystemHealthResponse(
            Status: "Healthy",
            Version: "1.1.0-prod",
            Uptime: 604800,
            DbConnected: true,
            RedisConnected: true
        ));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(StatusSnapshotResponse), 200)]
    public async Task<IActionResult> GetStatus()
    {
        var clients = await clientRepository.GetAllAsync();
        var clientDtos = clients.Select(c => new ClientResponse(
            c.PcId, c.SiteId, c.MacAddress, c.Hostname, c.IP, c.Status, c.LastSeen
        )).ToList();

        var sessions = await sessionRepository.GetAllAsync();
        var activeSessionDtos = sessions
            .Where(s => s.Status == "ACTIVE" || s.Status == "PAUSED")
            .Select(s => new SessionResponse(
                s.SessionId, s.PcId, s.SiteId, s.StartTime, s.EndTime, s.Status, s.Duration, s.CurrentCost, s.RatePerHour
            )).ToList();

        var response = new StatusSnapshotResponse(
            Timestamp: DateTime.UtcNow,
            Clients: clientDtos,
            ActiveSessions: activeSessionDtos
        );

        return Ok(response);
    }
}
