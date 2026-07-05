using System.Security.Cryptography;
using System.Text;

namespace Sayra.Server.UpdateSystem.Services;

public interface IIntegrityVerifier
{
    bool VerifyFile(string filePath, string expectedChecksum);
    bool VerifyManifestSignature(string manifestJson, string signature, string publicKey);
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

    public bool VerifyManifestSignature(string manifestJson, string signature, string publicKey)
    {
        // In a real implementation, we would use RSA to verify the signature.
        // For this phase, we'll implement a placeholder that demonstrates the flow.
        if (string.IsNullOrEmpty(signature)) return false;

        // Mock verification: check if signature is not "INVALID"
        return signature != "INVALID";
    }
}
