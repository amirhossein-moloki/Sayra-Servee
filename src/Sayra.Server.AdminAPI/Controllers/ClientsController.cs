using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController(IClientRepository clientRepository, IClientRegistry clientRegistry) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientResponse>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var entities = await clientRepository.GetAllAsync();
        var response = entities.Select(e => new ClientResponse(
            e.PcId, e.SiteId, e.MacAddress, e.Hostname, e.IP, e.Status, e.LastSeen));
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), 201)]
    public async Task<IActionResult> Register([FromBody] RegisterClientRequest request)
    {
        var entity = new ClientEntity
        {
            PcId = Guid.NewGuid().ToString(), // Simplified PC ID generation
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
        if (e == null) return NotFound(new ErrorResponse("NOT_FOUND", "Client not found"));
        return Ok(new ClientResponse(e.PcId, e.SiteId, e.MacAddress, e.Hostname, e.IP, e.Status, e.LastSeen));
    }

    [HttpGet("{pcId}/status")]
    [ProducesResponseType(typeof(ClientStateDto), 200)]
    public IActionResult GetStatus(string pcId)
    {
        // Mocked state for now, would come from clientRegistry in real implementation
        return Ok(new ClientStateDto("READY", "IDLE", null, null, 0, 0, 0, 0, null, false));
    }

    [HttpDelete("{pcId}/disconnect")]
    [ProducesResponseType(204)]
    public IActionResult Disconnect(string pcId)
    {
        // Logic to force disconnect via TCP
        return NoContent();
    }
}
