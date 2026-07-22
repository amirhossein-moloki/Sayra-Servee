using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class ProcessLaunchedMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    public ProcessLaunchedMessage()
    {
        Type = "PROCESS_LAUNCHED";
    }
}
