using System.Security.Cryptography;
using System.Text;

namespace Sayra.Server.Security;

public interface ISignatureService
{
    string Sign(string data, string key);
    bool Verify(string data, string signature, string key);
}

public class SignatureService : ISignatureService
{
    public string Sign(string data, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using HMACSHA256 hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string data, string signature, string key)
    {
        string expectedSignature = Sign(data, key);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature));
    }
}
