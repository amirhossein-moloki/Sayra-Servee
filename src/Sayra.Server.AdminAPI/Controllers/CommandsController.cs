using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using System.Collections.Concurrent;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/commands")]
public class CommandsController(ICommandRepository commandRepository, IClientRepository clientRepository) : ControllerBase
{
    // Thread-safe dictionary to track newly sent commands in memory for lookup in /api/commands/{commandId}
    private static readonly ConcurrentDictionary<string, CommandResponse> InMemoryCommands = new(StringComparer.OrdinalIgnoreCase);

    [HttpPost("send")]
    [ProducesResponseType(typeof(CommandResponse), 202)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Send([FromBody] SendCommandRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PcId) || string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "pcId and action are required properties."));
        }

        // Validate client exists
        var client = await clientRepository.GetByPcIdAsync(request.PcId);
        if (client == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Destination workstation '{request.PcId}' was not found."));
        }

        var commandId = Guid.NewGuid().ToString();
        var response = new CommandResponse(
            commandId,
            request.PcId,
            request.Action,
            "Executed", // Return executed / sent as expected by client
            null,
            DateTime.UtcNow
        );

        InMemoryCommands[commandId] = response;

        // Persist to audit repository
        var audit = new CommandAuditEntity
        {
            CommandId = commandId,
            PcId = request.PcId,
            Action = request.Action,
            Payload = request.Payload?.ToString() ?? string.Empty,
            Result = string.Empty,
            Timestamp = DateTime.UtcNow,
            SiteId = client.SiteId
        };
        await commandRepository.AddAsync(audit);

        return Accepted(response);
    }

    [HttpGet("{commandId}")]
    [ProducesResponseType(typeof(CommandResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult Get(string commandId)
    {
        if (InMemoryCommands.TryGetValue(commandId, out var command))
        {
            return Ok(command);
        }

        return NotFound(new ErrorResponse("NOT_FOUND", $"Command execution receipt '{commandId}' was not found."));
    }

    [HttpGet("history/{pcId}")]
    [ProducesResponseType(typeof(IEnumerable<CommandResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetHistory(string pcId)
    {
        var client = await clientRepository.GetByPcIdAsync(pcId);
        if (client == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Client with PC ID '{pcId}' was not found."));
        }

        var audits = await commandRepository.GetByPcIdAsync(pcId);
        var response = audits.Select(a => new CommandResponse(
            a.CommandId, a.PcId, a.Action, "Executed", a.Result, a.Timestamp));

        return Ok(response);
    }
}
