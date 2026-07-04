namespace Sayra.Server.Shared.Messages;

public class SecureEnvelope
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
}
