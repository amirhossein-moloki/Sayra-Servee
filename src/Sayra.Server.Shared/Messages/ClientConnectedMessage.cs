using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class ClientConnectedMessage : BaseMessage
{
    [JsonPropertyName("pcId")]
    public string PcId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string IPAddress { get; set; } = string.Empty;

    public ClientConnectedMessage()
    {
        Type = "CLIENT_CONNECTED";
    }
}
