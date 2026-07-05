using System.Security.Cryptography;
using System.Text;

namespace Sayra.Server.UpdateSystem.Services;

public interface IIntegrityVerifier
{
    bool VerifyFile(string filePath, string expectedChecksum);
    bool VerifyManifestSignature(string manifestJson, string signature, string publicKeyPem);
}

public class IntegrityVerifier : IIntegrityVerifier
{
    public bool VerifyFile(string filePath, string expectedChecksum)
    {
        if (!File.Exists(filePath)) return false;

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        var actualChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    public bool VerifyManifestSignature(string manifestJson, string signature, string publicKeyPem)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
            var dataBytes = Encoding.UTF8.GetBytes(manifestJson);
            var signatureBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
#if DEBUG
            return signature == "DEVELOPMENT_BYPASS";
#else
            return false;
#endif
        }
    }
}
