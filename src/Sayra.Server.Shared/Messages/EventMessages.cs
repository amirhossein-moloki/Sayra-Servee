using System;
using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class AuthenticationStartedMessage : BaseMessage
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class EventUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = Array.Empty<string>();

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("lastLogin")]
    public string? LastLogin { get; set; }

    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = string.Empty;

    [JsonPropertyName("preferredTheme")]
    public string PreferredTheme { get; set; } = string.Empty;

    [JsonPropertyName("stationId")]
    public string? StationId { get; set; }
}

public class AuthenticationSucceededMessage : BaseMessage
{
    [JsonPropertyName("user")]
    public EventUserDto User { get; set; } = new();

    [JsonPropertyName("authenticationType")]
    public string AuthenticationType { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}

public class AuthenticationFailedMessage : BaseMessage
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class LogoutUserDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class LogoutStartedMessage : BaseMessage
{
    [JsonPropertyName("user")]
    public LogoutUserDto User { get; set; } = new();
}

public class LogoutCompletedMessage : BaseMessage
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class SessionExpiredMessage : BaseMessage
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class GameLaunchingMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class GameStartedMessage : BaseMessage
{
    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class GameExitedMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;
}

public class GameCrashedMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class LaunchFailedMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class SecurityBreachDetectedMessage : BaseMessage
{
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

public class BillingUpdateMessage : BaseMessage
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("ratePerHour")]
    public decimal RatePerHour { get; set; }

    [JsonPropertyName("remainingCredits")]
    public decimal RemainingCredits { get; set; }
}
