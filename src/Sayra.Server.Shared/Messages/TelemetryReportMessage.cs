using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class TelemetryReportMessage : BaseMessage
{
    [JsonPropertyName("cpu")]
    public double Cpu { get; set; }

    [JsonPropertyName("ram")]
    public double Ram { get; set; }

    [JsonPropertyName("uptime")]
    public long Uptime { get; set; }

    [JsonPropertyName("runningGameName")]
    public string? RunningGameName { get; set; }

    [JsonPropertyName("runningGamePid")]
    public int? RunningGamePid { get; set; }

    [JsonPropertyName("runningGameCpu")]
    public double? RunningGameCpu { get; set; }

    [JsonPropertyName("runningGameRam")]
    public double? RunningGameRam { get; set; }

    [JsonPropertyName("runningGameDurationSeconds")]
    public double? RunningGameDurationSeconds { get; set; }

    [JsonPropertyName("totalLaunches")]
    public int? TotalLaunches { get; set; }

    [JsonPropertyName("totalCrashes")]
    public int? TotalCrashes { get; set; }

    [JsonPropertyName("totalRestarts")]
    public int? TotalRestarts { get; set; }

    public TelemetryReportMessage()
    {
        Type = "TELEMETRY_REPORT";
    }
}
