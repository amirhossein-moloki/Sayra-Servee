using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Licensing.Services;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("license")]
public class LicenseController(LicenseService licenseService) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(LicenseStatusResponse), 200)]
    public IActionResult Validate([FromBody] ValidateLicenseRequest request)
    {
        return Ok(new LicenseStatusResponse(true, "Standard", DateTime.UtcNow.AddYears(1), "HW-ID-123", "Site 1", "User 1"));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(LicenseStatusResponse), 200)]
    public IActionResult GetStatus()
    {
        return Ok(new LicenseStatusResponse(true, "Standard", DateTime.UtcNow.AddYears(1), "HW-ID-123", "Site 1", "User 1"));
    }
}
