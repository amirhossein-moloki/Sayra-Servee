using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("commands")]
public class CommandsController(ICommandRepository commandRepository) : ControllerBase
{
    [HttpPost("send")]
    [ProducesResponseType(typeof(CommandResponse), 202)]
    public IActionResult Send([FromBody] SendCommandRequest request)
    {
        var response = new CommandResponse(
            Guid.NewGuid().ToString(),
            request.PcId,
            request.Action,
            "Pending",
            null,
            DateTime.UtcNow
        );
        return Accepted(response);
    }

    [HttpGet("{commandId}")]
    [ProducesResponseType(typeof(CommandResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult Get(string commandId)
    {
        // Lookup in audit repository
        return NotFound(new ErrorResponse("NOT_FOUND", "Command not found"));
    }

    [HttpGet("history/{pcId}")]
    [ProducesResponseType(typeof(IEnumerable<CommandResponse>), 200)]
    public async Task<IActionResult> GetHistory(string pcId)
    {
        var audits = await commandRepository.GetByPcIdAsync(pcId);
        var response = audits.Select(a => new CommandResponse(
            a.CommandId, a.PcId, a.Action, "Executed", a.Result, a.Timestamp));
        return Ok(response);
    }
}
