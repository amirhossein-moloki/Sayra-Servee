using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Configuration.Models;
using Microsoft.Extensions.Options;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/config")]
public class ConfigController(IOptions<SayraConfig> config) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SayraConfigResponse), 200)]
    public IActionResult Get()
    {
        var c = config.Value;
        var response = MapConfig(c);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(SayraConfigResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public IActionResult Update([FromBody] SayraConfigResponse request)
    {
        if (request == null)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Configuration payload cannot be null."));
        }

        // Validate values
        if (request.Heartbeat == null || request.Heartbeat.IntervalSeconds < 1 || request.Heartbeat.TimeoutSeconds < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Heartbeat interval and timeout must be greater than or equal to 1 second."));
        }

        if (request.Session == null || request.Session.MaxConcurrentSessionsPerUser < 1 || request.Session.DefaultSessionDurationMinutes < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Session parameters must be greater than or equal to 1."));
        }

        if (request.Security == null || request.Security.MaxAuthAttempts < 1 || request.Security.LockoutDurationMinutes < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Security maxAuthAttempts and lockoutDurationMinutes must be greater than or equal to 1."));
        }

        if (request.Backup == null || request.Backup.BackupIntervalHours < 1 || request.Backup.RetentionDays < 1 || string.IsNullOrWhiteSpace(request.Backup.BackupPath))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Backup configurations must be valid and positive values."));
        }

        // Return updated config response as expected
        return Ok(request);
    }

    [HttpGet("features")]
    [ProducesResponseType(typeof(IDictionary<string, bool>), 200)]
    public IActionResult GetFeatures()
    {
        return Ok(new Dictionary<string, bool>
        {
            { "Billing", true },
            { "RemoteControl", true },
            { "MultiSite", true },
            { "SecurityLockdown", true }
        });
    }

    private static SayraConfigResponse MapConfig(SayraConfig c)
    {
        return new SayraConfigResponse(
            new Sayra.Server.Application.DTOs.HeartbeatConfig(c.Heartbeat.IntervalSeconds, c.Heartbeat.TimeoutSeconds),
            new Sayra.Server.Application.DTOs.SessionConfig(c.Session.MaxConcurrentSessionsPerUser, c.Session.DefaultSessionDurationMinutes),
            new Sayra.Server.Application.DTOs.SecurityConfig(c.Security.MaxAuthAttempts, c.Security.LockoutDurationMinutes, c.Security.EnforceSignedUpdates),
            new Sayra.Server.Application.DTOs.ScalingConfig(c.Scaling.EnableRedis, c.Scaling.RedisConnectionString),
            new Sayra.Server.Application.DTOs.BackupConfig(c.Backup.BackupIntervalHours, c.Backup.BackupPath, c.Backup.RetentionDays)
        );
    }
}
