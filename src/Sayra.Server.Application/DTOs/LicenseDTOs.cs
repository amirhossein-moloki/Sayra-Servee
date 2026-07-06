namespace Sayra.Server.Application.DTOs;

public record ValidateLicenseRequest(string LicenseKey);

public record LicenseStatusResponse(
    bool IsValid,
    string Tier,
    DateTime ExpiryDate,
    string HardwareId,
    string SiteName,
    string IssuedTo
);
