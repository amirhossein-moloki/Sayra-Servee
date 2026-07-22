using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class CommandMessage : BaseMessage
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    [JsonPropertyName("commandName")]
    public string CommandName { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public string Parameters { get; set; } = string.Empty;

    public CommandMessage()
    {
        Type = "COMMAND";
    }
}
