namespace Sayra.Server.Application.DTOs;

public record ErrorResponse(
    string Code,
    string Message,
    string? Details = null
);
