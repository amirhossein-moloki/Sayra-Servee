using System.ComponentModel.DataAnnotations;

namespace Sayra.Server.Application.DTOs;

public record LoginRequest(
    [Required(ErrorMessage = "Username is required")] string Username,
    [Required(ErrorMessage = "Password is required")] string Password
);

public record AuthTokenResponse(
    [Required] string AccessToken,
    [Required] int ExpiresIn,
    [Required] string TokenType = "Bearer"
);
