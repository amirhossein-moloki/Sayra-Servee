using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class ExecutionResultMessage : BaseMessage
{
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    [JsonPropertyName("pcId")]
    public string PcId { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // Value MUST be 'Executed' or 'Failed'

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    public ExecutionResultMessage()
    {
        Type = "EXECUTION_RESULT";
    }
}
