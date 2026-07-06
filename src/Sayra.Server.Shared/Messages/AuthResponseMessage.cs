using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class AuthResponseMessage : BaseMessage
{
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("session_key")]
    public string SessionKey { get; set; } = string.Empty;

    public AuthResponseMessage()
    {
        Type = "AUTH_RESPONSE";
    }
}
