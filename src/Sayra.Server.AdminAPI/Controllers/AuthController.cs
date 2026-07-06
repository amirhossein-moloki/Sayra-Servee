using Microsoft.AspNetCore.Mvc;
using Sayra.Server.Application.DTOs;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Authentication;

namespace Sayra.Server.AdminAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAdminUserRepository adminUserRepository) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await adminUserRepository.GetByUsernameAsync(request.Username);

        // Simplified check for now. In production, use a secure password hasher.
        if (user != null && user.PasswordHash == request.Password)
        {
            return Ok(new AuthTokenResponse("dummy-jwt-token", 3600));
        }

        return Unauthorized(new ErrorResponse("AUTH_FAILED", "Invalid username or password"));
    }
}
