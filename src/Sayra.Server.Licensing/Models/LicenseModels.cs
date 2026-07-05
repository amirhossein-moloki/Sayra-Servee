namespace Sayra.Server.Licensing.Models;

public enum LicenseTier
{
    Trial,
    Standard,
    Pro,
    Enterprise
}

public class LicenseInfo
{
    public string HardwareId { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string IssuedTo { get; set; } = string.Empty;
}

public class SignedLicense
{
    public string DataJson { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
