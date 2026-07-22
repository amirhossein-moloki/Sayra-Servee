using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

[JsonDerivedType(typeof(HeartbeatMessage), typeDiscriminator: "HEARTBEAT")]
[JsonDerivedType(typeof(PingMessage), typeDiscriminator: "PING")]
[JsonDerivedType(typeof(CommandMessage), typeDiscriminator: "COMMAND")]
[JsonDerivedType(typeof(AuthMessage), typeDiscriminator: "AUTH")]
[JsonDerivedType(typeof(ClientConnectedMessage), typeDiscriminator: "CLIENT_CONNECTED")]
[JsonDerivedType(typeof(ClientDisconnectedMessage), typeDiscriminator: "CLIENT_DISCONNECTED")]
[JsonDerivedType(typeof(AuthChallengeMessage), typeDiscriminator: "AUTH_CHALLENGE")]
[JsonDerivedType(typeof(AuthResponseMessage), typeDiscriminator: "AUTH_RESPONSE")]
[JsonDerivedType(typeof(AuthStatusMessage), typeDiscriminator: "AUTH_STATUS")]
[JsonDerivedType(typeof(AuthenticationStartedMessage), typeDiscriminator: "AuthenticationStarted")]
[JsonDerivedType(typeof(AuthenticationSucceededMessage), typeDiscriminator: "AuthenticationSucceeded")]
[JsonDerivedType(typeof(AuthenticationFailedMessage), typeDiscriminator: "AuthenticationFailed")]
[JsonDerivedType(typeof(LogoutStartedMessage), typeDiscriminator: "LogoutStarted")]
[JsonDerivedType(typeof(LogoutCompletedMessage), typeDiscriminator: "LogoutCompleted")]
[JsonDerivedType(typeof(SessionExpiredMessage), typeDiscriminator: "SessionExpired")]
[JsonDerivedType(typeof(GameLaunchingMessage), typeDiscriminator: "GameLaunching")]
[JsonDerivedType(typeof(GameStartedMessage), typeDiscriminator: "GameStarted")]
[JsonDerivedType(typeof(GameExitedMessage), typeDiscriminator: "GameExited")]
[JsonDerivedType(typeof(GameCrashedMessage), typeDiscriminator: "GameCrashed")]
[JsonDerivedType(typeof(LaunchFailedMessage), typeDiscriminator: "LaunchFailed")]
[JsonDerivedType(typeof(SecurityBreachDetectedMessage), typeDiscriminator: "SECURITY_BREACH_DETECTED")]
[JsonDerivedType(typeof(BillingUpdateMessage), typeDiscriminator: "BILLING_UPDATE")]
public class BaseMessage
{
    public string Type { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public object Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
