namespace Sayra.Server.Shared.Messages;

public class SecureEnvelope
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
