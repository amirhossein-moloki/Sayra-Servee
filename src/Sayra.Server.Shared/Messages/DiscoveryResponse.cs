namespace Sayra.Server.Shared.Messages;

public class DiscoveryResponse
{
    public string Type { get; set; } = "SAYRA_SERVER_RESPONSE";
    public string ServerId { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int TcpPort { get; set; }
    public int ApiPort { get; set; }
    public string Version { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Signature { get; set; } = string.Empty;
}
