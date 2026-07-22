using System.Text.Json.Serialization;

namespace Sayra.Server.Shared.Messages;

public class SessionModel
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("pcId")]
    public string PcId { get; set; } = string.Empty;

    [JsonPropertyName("siteId")]
    public string SiteId { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("ratePerHour")]
    public double RatePerHour { get; set; }

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;
}
