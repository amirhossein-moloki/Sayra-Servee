using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sayra.Server.Licensing.Models;

namespace Sayra.Server.Licensing.Services;

public interface ILicenseService
{
    bool ValidateLicense(string licensePath, out LicenseInfo? licenseInfo);
    string GenerateLicenseRequest();
}

public class LicenseService : ILicenseService
{
    private readonly IHardwareFingerprintService _fingerprintService;

    // In a real production system, this would be loaded from a secure embedded resource
    private const string PUBLIC_KEY_PEM =
        "-----BEGIN PUBLIC KEY-----\n" +
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA6YjUj5f...[REAL_KEY_STRUCTURE]...AQAB\n" +
        "-----END PUBLIC KEY-----";

    public LicenseService(IHardwareFingerprintService fingerprintService)
    {
        _fingerprintService = fingerprintService;
    }

    public bool ValidateLicense(string licensePath, out LicenseInfo? licenseInfo)
    {
        licenseInfo = null;
        if (!File.Exists(licensePath)) return false;

        try
        {
            var json = File.ReadAllText(licensePath);
            var signedLicense = JsonSerializer.Deserialize<SignedLicense>(json);
            if (signedLicense == null) return false;

            if (!VerifySignature(signedLicense.DataJson, signedLicense.Signature))
                return false;

            var info = JsonSerializer.Deserialize<LicenseInfo>(signedLicense.DataJson);
            if (info == null) return false;

            var currentHardwareId = _fingerprintService.GetHardwareId();
            if (info.HardwareId != currentHardwareId) return false;

            if (info.ExpiryDate < DateTime.UtcNow) return false;

            licenseInfo = info;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateLicenseRequest()
    {
        var hardwareId = _fingerprintService.GetHardwareId();
        var request = new
        {
            HardwareId = hardwareId,
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName
        };
        return JsonSerializer.Serialize(request);
    }

    private bool VerifySignature(string data, string signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(PUBLIC_KEY_PEM.ToCharArray());
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            // For Phase 6 development, if the REAL signature fails (as expected with a mock PEM),
            // we return true ONLY if signature is "DEVELOPMENT_BYPASS" and we are in a non-production environment.
            // In a real release build, this fallback would be removed via #if DEBUG
#if DEBUG
            return signature == "DEVELOPMENT_BYPASS";
#else
            return false;
#endif
        }
    }
}
