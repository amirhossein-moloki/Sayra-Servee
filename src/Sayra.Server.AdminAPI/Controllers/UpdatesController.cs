using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.UpdateSystem.Services;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("api/updates")]
public class UpdatesController(UpdateDistributor updateDistributor) : ControllerBase
{
    [HttpGet("manifest")]
    [ProducesResponseType(typeof(UpdateManifest), 200)]
    public IActionResult GetManifest()
    {
        return Ok(new UpdateManifest("1.1.0", "New version", "http://local/pkg.zip", "sha256...", "sig...", true, DateTime.UtcNow));
    }
}
