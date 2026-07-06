namespace Sayra.Server.Shared.Messages;

public class DiscoveryRequest : BaseMessage
{
    public string Nonce { get; set; } = string.Empty;

    public DiscoveryRequest()
    {
        Type = "DISCOVER_SAYRA_SERVER";
    }
}
