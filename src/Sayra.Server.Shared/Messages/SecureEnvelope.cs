namespace Sayra.Server.Shared.Messages;

public class SecureEnvelope
{
    public string Payload { get; set; } = string.Empty; // AES-256 encrypted JSON
    public string HMAC { get; set; } = string.Empty;    // HMAC-SHA256 signature
    public string IV { get; set; } = string.Empty;      // Initialization Vector
}
