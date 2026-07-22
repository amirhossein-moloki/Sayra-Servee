using System;

namespace Sayra.Server.EventBus.Events;

public class EventUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public string? Avatar { get; set; }
    public string? LastLogin { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public string PreferredTheme { get; set; } = string.Empty;
    public string? StationId { get; set; }
}

public class LogoutUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public record AuthenticationStartedEvent(string ClientId, string Username, string Timestamp) : BaseEvent;

public record AuthenticationSucceededEvent(string ClientId, EventUserDto User, string AuthenticationType, string SessionId, string Timestamp) : BaseEvent;

public record AuthenticationFailedEvent(string ClientId, string Username, string Reason, string Timestamp) : BaseEvent;

public record LogoutStartedEvent(string ClientId, LogoutUserDto User, string Timestamp) : BaseEvent;

public record LogoutCompletedEvent(string ClientId, string? Username, string Timestamp) : BaseEvent;

public record SessionExpiredEvent(string ClientId, string SessionId, string? Username, string Timestamp) : BaseEvent;

public record GameLaunchingEvent(string ClientId, string GameId, string Name) : BaseEvent;

public record GameStartedEvent(string ClientId, int Pid, string GameId, string Name) : BaseEvent;

public record GameExitedEvent(string ClientId, string GameId, string Name, int ExitCode, string Duration) : BaseEvent;

public record GameCrashedEvent(string ClientId, string GameId, string Name, int ExitCode, string Reason) : BaseEvent;

public record LaunchFailedEvent(string ClientId, string GameId, string Name, string Reason) : BaseEvent;

public record SecurityBreachDetectedEvent(string ClientId, string Severity, string Description, string? Details) : BaseEvent;

public record BillingUpdateEvent(string ClientId, string SessionId, decimal RatePerHour, decimal RemainingCredits) : BaseEvent;
