using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class ProcessExitedMessage : BaseMessage
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    public ProcessExitedMessage()
    {
        Type = "PROCESS_EXITED";
    }
}
