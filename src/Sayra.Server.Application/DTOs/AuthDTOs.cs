namespace Sayra.Server.Application.DTOs;

public record LoginRequest(string Username, string Password);
public record AuthTokenResponse(string AccessToken, int ExpiresIn, string TokenType = "Bearer");
