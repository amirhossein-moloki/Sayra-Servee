using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Licensing.Services;

namespace Sayra.Server.AdminAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/license")]
public class LicenseController(LicenseService licenseService) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(LicenseStatusResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public IActionResult Validate([FromBody] ValidateLicenseRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LicenseKey))
        {
            return BadRequest(new ErrorResponse("BAD_REQUEST", "licenseKey is a required property."));
        }

        var isValid = request.LicenseKey.StartsWith("SAYRA-", StringComparison.OrdinalIgnoreCase);

        return Ok(new LicenseStatusResponse(
            IsValid: isValid,
            Tier: "Pro",
            ExpiryDate: DateTime.UtcNow.AddYears(1),
            HardwareId: "BIOS-9021-3912-3021",
            SiteName: "Sayra Gaming Club Hub",
            IssuedTo: "Amir Mohammadi"
        ));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(LicenseStatusResponse), 200)]
    public IActionResult GetStatus()
    {
        return Ok(new LicenseStatusResponse(
            IsValid: true,
            Tier: "Pro",
            ExpiryDate: DateTime.UtcNow.AddYears(1),
            HardwareId: "BIOS-9021-3912-3021",
            SiteName: "Sayra Gaming Club Hub",
            IssuedTo: "Amir Mohammadi"
        ));
    }
}
