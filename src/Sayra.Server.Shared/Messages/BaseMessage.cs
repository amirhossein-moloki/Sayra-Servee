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
[JsonDerivedType(typeof(TelemetryReportMessage), typeDiscriminator: "TELEMETRY_REPORT")]
[JsonDerivedType(typeof(ProcessLaunchedMessage), typeDiscriminator: "PROCESS_LAUNCHED")]
[JsonDerivedType(typeof(ProcessExitedMessage), typeDiscriminator: "PROCESS_EXITED")]
[JsonDerivedType(typeof(ExecutionResultMessage), typeDiscriminator: "EXECUTION_RESULT")]
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
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
public class BaseMessage
{
    private string _type = string.Empty;
    public string Type
    {
        get
        {
            if (string.IsNullOrEmpty(_type))
            {
                _type = this switch
                {
                    HeartbeatMessage => "HEARTBEAT",
                    PingMessage => "PING",
                    CommandMessage => "COMMAND",
                    AuthMessage => "AUTH",
                    ClientConnectedMessage => "CLIENT_CONNECTED",
                    ClientDisconnectedMessage => "CLIENT_DISCONNECTED",
                    AuthChallengeMessage => "AUTH_CHALLENGE",
                    AuthResponseMessage => "AUTH_RESPONSE",
                    AuthStatusMessage => "AUTH_STATUS",
                    TelemetryReportMessage => "TELEMETRY_REPORT",
                    ProcessLaunchedMessage => "PROCESS_LAUNCHED",
                    ProcessExitedMessage => "PROCESS_EXITED",
                    ExecutionResultMessage => "EXECUTION_RESULT",
                    AuthenticationStartedMessage => "AuthenticationStarted",
                    AuthenticationSucceededMessage => "AuthenticationSucceeded",
                    AuthenticationFailedMessage => "AuthenticationFailed",
                    LogoutStartedMessage => "LogoutStarted",
                    LogoutCompletedMessage => "LogoutCompleted",
                    SessionExpiredMessage => "SessionExpired",
                    GameLaunchingMessage => "GameLaunching",
                    GameStartedMessage => "GameStarted",
                    GameExitedMessage => "GameExited",
                    GameCrashedMessage => "GameCrashed",
                    LaunchFailedMessage => "LaunchFailed",
                    SecurityBreachDetectedMessage => "SECURITY_BREACH_DETECTED",
                    BillingUpdateMessage => "BILLING_UPDATE",
                    _ => string.Empty
                };
            }
            return _type;
        }
        set => _type = value;
    }

    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public object Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
