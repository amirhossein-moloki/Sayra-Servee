using Microsoft.Extensions.Options;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Authentication;

public interface IAuthService
{
    AuthChallengeMessage InitiateHandshake(string clientId);
    (bool Success, string SessionKey) Authenticate(AuthResponseMessage response);
}

public class AuthService : IAuthService
{
    private readonly IChallengeGenerator _challengeGenerator;
    private readonly IAuthSessionManager _authSessionManager;
    private readonly ISignatureService _signatureService;
    private readonly SecurityOptions _options;

    public AuthService(
        IChallengeGenerator challengeGenerator,
        IAuthSessionManager authSessionManager,
        ISignatureService signatureService,
        IOptions<SecurityOptions> options)
    {
        _challengeGenerator = challengeGenerator;
        _authSessionManager = authSessionManager;
        _signatureService = signatureService;
        _options = options.Value;
    }

    public AuthChallengeMessage InitiateHandshake(string clientId)
    {
        var challenge = _challengeGenerator.Generate();
        _authSessionManager.SetPendingChallenge(clientId, challenge);

        return new AuthChallengeMessage
        {
            ClientId = clientId,
            Challenge = challenge
        };
    }

    public (bool Success, string SessionKey) Authenticate(AuthResponseMessage response)
    {
        var pendingChallenge = _authSessionManager.GetPendingChallenge(response.ClientId);
        if (pendingChallenge == null)
        {
            return (false, string.Empty);
        }

        // Expected response is HMAC(challenge + nonce, masterKey)
        string dataToVerify = $"{pendingChallenge}{response.Nonce}";
        bool isValid = _signatureService.Verify(dataToVerify, response.Response, _options.MasterKey);

        if (isValid)
        {
            _authSessionManager.RemovePendingChallenge(response.ClientId);
            // Derive a session key
            string sessionKey = _signatureService.Sign($"{_options.MasterKey}{response.Nonce}", response.ClientId);
            return (true, sessionKey);
        }

        return (false, string.Empty);
    }
}
