using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Monitoring.Interfaces;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("monitoring")]
public class MonitoringController(
    ITelemetryRepository telemetryRepository,
    IMetricsService metricsService) : ControllerBase
{
    [HttpGet("telemetry/{pcId}")]
    [ProducesResponseType(typeof(IEnumerable<TelemetryResponse>), 200)]
    public async Task<IActionResult> GetTelemetry(string pcId, [FromQuery] int limit = 100)
    {
        var entities = await telemetryRepository.GetByPcIdAsync(pcId, limit);
        var response = entities.Select(e => new TelemetryResponse(e.CPU, e.RAM, e.Uptime, e.Timestamp));
        return Ok(response);
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthResponse), 200)]
    public IActionResult GetHealth()
    {
        return Ok(new SystemHealthResponse("Healthy", "1.1.0", 3600, true, false));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(StatusSnapshotResponse), 200)]
    public IActionResult GetStatus()
    {
        return Ok(new StatusSnapshotResponse(DateTime.UtcNow, [], []));
    }
}
