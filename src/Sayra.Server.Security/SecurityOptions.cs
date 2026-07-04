namespace Sayra.Server.Security;

public class SecurityOptions
{
    public string MasterKey { get; set; } = "SayraMasterKey2024"; // Default for development
    public int MaxTimestampDriftSeconds { get; set; } = 10;
}
