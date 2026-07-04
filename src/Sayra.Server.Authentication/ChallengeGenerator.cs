using System.Security.Cryptography;

namespace Sayra.Server.Authentication;

public interface IChallengeGenerator
{
    string Generate();
}

public class ChallengeGenerator : IChallengeGenerator
{
    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
