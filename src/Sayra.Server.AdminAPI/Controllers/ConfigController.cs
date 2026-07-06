using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Configuration.Models;
using Microsoft.Extensions.Options;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("config")]
public class ConfigController(IOptions<SayraConfig> config) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SayraConfigResponse), 200)]
    public IActionResult Get()
    {
        var c = config.Value;
        var response = new SayraConfigResponse(
            new Sayra.Server.Application.DTOs.HeartbeatConfig(c.Heartbeat.IntervalSeconds, c.Heartbeat.TimeoutSeconds),
            new Sayra.Server.Application.DTOs.SessionConfig(c.Session.MaxConcurrentSessionsPerUser, c.Session.DefaultSessionDurationMinutes),
            new Sayra.Server.Application.DTOs.SecurityConfig(c.Security.MaxAuthAttempts, c.Security.LockoutDurationMinutes, c.Security.EnforceSignedUpdates),
            new Sayra.Server.Application.DTOs.ScalingConfig(c.Scaling.EnableRedis, c.Scaling.RedisConnectionString),
            new Sayra.Server.Application.DTOs.BackupConfig(c.Backup.BackupIntervalHours, c.Backup.BackupPath, c.Backup.RetentionDays)
        );
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(SayraConfigResponse), 200)]
    public IActionResult Update([FromBody] SayraConfigResponse request)
    {
        return Ok(request);
    }

    [HttpGet("features")]
    [ProducesResponseType(typeof(IDictionary<string, bool>), 200)]
    public IActionResult GetFeatures()
    {
        return Ok(new Dictionary<string, bool> { { "Billing", true }, { "RemoteControl", true } });
    }
}
