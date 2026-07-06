using System.Text.Json.Serialization;

namespace Sayra.Server.Application.DTOs;

public record SendCommandRequest(
    string PcId,
    string Action,
    object? Payload
);

public record RunAppPayload(string GameId);

public record KillAppPayload(int? Pid, string? Name);

public record CommandResponse(
    string CommandId,
    string PcId,
    string Action,
    string Status,
    object? Result,
    DateTime Timestamp
);

public record ProcessInfo(int Pid, string Name, string? GameId);
