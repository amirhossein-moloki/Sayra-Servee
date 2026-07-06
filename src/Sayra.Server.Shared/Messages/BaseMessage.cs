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
[JsonDerivedType(typeof(DiscoveryRequest), typeDiscriminator: "DISCOVER_SAYRA_SERVER")]
public class BaseMessage
{
    public string Type { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
