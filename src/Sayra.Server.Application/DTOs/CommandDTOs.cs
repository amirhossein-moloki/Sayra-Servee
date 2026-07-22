using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sayra.Server.Application.DTOs;

public record SendCommandRequest(
    [Required(ErrorMessage = "pcId is required")] string PcId,
    [Required(ErrorMessage = "action is required")] string Action,
    object? Payload
);

public record RunAppPayload(
    [Required] string GameId
);

public record KillAppPayload(
    int? Pid,
    string? Name
);

public record CommandResponse(
    [Required] string CommandId,
    [Required] string PcId,
    [Required] string Action,
    [Required] string Status,
    object? Result,
    [Required] DateTime Timestamp
);

public record ProcessInfo(
    [Required] int Pid,
    [Required] string Name,
    string? GameId
);
