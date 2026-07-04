using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

[JsonDerivedType(typeof(HeartbeatMessage), typeDiscriminator: "HEARTBEAT")]
[JsonDerivedType(typeof(PingMessage), typeDiscriminator: "PING")]
[JsonDerivedType(typeof(CommandMessage), typeDiscriminator: "COMMAND")]
[JsonDerivedType(typeof(AuthMessage), typeDiscriminator: "AUTH")]
[JsonDerivedType(typeof(ClientConnectedMessage), typeDiscriminator: "CLIENT_CONNECTED")]
[JsonDerivedType(typeof(ClientDisconnectedMessage), typeDiscriminator: "CLIENT_DISCONNECTED")]
public class BaseMessage
{
    public string Type { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
