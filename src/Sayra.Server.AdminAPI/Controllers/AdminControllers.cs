using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.Interfaces;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IClientRepository clientRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await clientRepository.GetAllAsync());

    [HttpGet("{pcId}")]
    public async Task<IActionResult> Get(string pcId)
    {
        var client = await clientRepository.GetByPcIdAsync(pcId);
        return client == null ? NotFound() : Ok(client);
    }
}

[ApiController]
[Route("[controller]")]
public class SessionsController(ISessionRepository sessionRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await sessionRepository.GetAllAsync());

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> Get(string sessionId)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId);
        return session == null ? NotFound() : Ok(session);
    }
}

[ApiController]
[Route("[controller]")]
public class TelemetryController(ITelemetryRepository telemetryRepository) : ControllerBase
{
    [HttpGet("{pcId}")]
    public async Task<IActionResult> GetByPcId(string pcId, [FromQuery] int limit = 100) =>
        Ok(await telemetryRepository.GetByPcIdAsync(pcId, limit));
}

[ApiController]
[Route("[controller]")]
public class CommandsController(ICommandRepository commandRepository) : ControllerBase
{
    [HttpGet("{pcId}")]
    public async Task<IActionResult> GetByPcId(string pcId) =>
        Ok(await commandRepository.GetByPcIdAsync(pcId));

    [HttpPost("send")]
    public IActionResult SendCommand([FromBody] object command)
    {
        // design only for now as per requirements
        return Accepted(new { Message = "Command queued for delivery" });
    }
}
