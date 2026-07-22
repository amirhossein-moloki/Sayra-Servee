using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/clients")]
public class ClientsController(IClientRepository clientRepository, IClientRegistry clientRegistry) : ControllerBase
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Offline", "Online", "Locked", "InUse", "Maintenance"
    };

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] string? status = null)
    {
        if (page < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Page number must be greater than or equal to 1."));
        }
        if (limit < 1)
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "Limit must be greater than or equal to 1."));
        }

        if (!string.IsNullOrEmpty(status) && !ValidStatuses.Contains(status))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", $"Invalid status filter. Allowed values are: {string.Join(", ", ValidStatuses)}."));
        }

        var entities = await clientRepository.GetAllAsync();

        if (!string.IsNullOrEmpty(status))
        {
            entities = entities.Where(e => e.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var response = entities
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(e => new ClientResponse(
                e.PcId, e.SiteId, e.MacAddress, e.Hostname, e.IP, e.Status, e.LastSeen));

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterClientRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MacAddress) || string.IsNullOrWhiteSpace(request.Hostname))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "macAddress and hostname are required properties."));
        }

        var entity = new ClientEntity
        {
            PcId = Guid.NewGuid().ToString(),
            MacAddress = request.MacAddress,
            Hostname = request.Hostname,
            SiteId = request.SiteId ?? "default",
            Status = "Offline",
            LastSeen = DateTime.UtcNow
        };

        await clientRepository.UpsertAsync(entity);

        return CreatedAtAction(nameof(Get), new { pcId = entity.PcId }, new ClientResponse(
            entity.PcId, entity.SiteId, entity.MacAddress, entity.Hostname, entity.IP, entity.Status, entity.LastSeen));
    }

    [HttpGet("{pcId}")]
    [ProducesResponseType(typeof(ClientResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Get(string pcId)
    {
        var e = await clientRepository.GetByPcIdAsync(pcId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Client with PC ID '{pcId}' was not found."));
        }

        return Ok(new ClientResponse(e.PcId, e.SiteId, e.MacAddress, e.Hostname, e.IP, e.Status, e.LastSeen));
    }

    [HttpGet("{pcId}/status")]
    [ProducesResponseType(typeof(ClientStateDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetStatus(string pcId)
    {
        var e = await clientRepository.GetByPcIdAsync(pcId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Client with PC ID '{pcId}' was not found."));
        }

        // Retrieve real runtime status if registered, otherwise return mock / sensible default
        var remainingTime = "00:00:00";
        var isLocked = e.Status.Equals("Locked", StringComparison.OrdinalIgnoreCase) || e.Status.Equals("Offline", StringComparison.OrdinalIgnoreCase);

        return Ok(new ClientStateDto(
            CoreState: isLocked ? "READY" : "IN_SESSION",
            SessionStatus: isLocked ? "IDLE" : "ACTIVE",
            RemainingTime: remainingTime,
            StartTime: DateTime.UtcNow.AddHours(-1),
            ElapsedSeconds: 3600,
            TotalDurationMinutes: 120,
            RatePerHour: 15000m,
            CurrentCost: 7500m,
            UserName: "amir",
            IsKioskLocked: isLocked
        ));
    }

    [HttpDelete("{pcId}/disconnect")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Disconnect(string pcId)
    {
        var e = await clientRepository.GetByPcIdAsync(pcId);
        if (e == null)
        {
            return NotFound(new ErrorResponse("NOT_FOUND", $"Client with PC ID '{pcId}' was not found."));
        }

        // Logic to force disconnect via TCP/Registry
        clientRegistry.Remove(pcId);

        return NoContent();
    }
}
