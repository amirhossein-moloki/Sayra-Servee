using Xunit;
using Moq;
using Sayra.Server.Authentication;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;
using System.Security.Cryptography;
using System.Text;

namespace Sayra.Server.IntegrationTests;

public class AuthHandshakeTests
{
    [Fact]
    public void FullHandshake_Should_Succeed()
    {
        // Arrange
        var signatureService = new SignatureService();
        var challengeGenerator = new ChallengeGenerator();
        var authSessionManager = new AuthSessionManager();
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, options);

        string clientId = "PC-01";
        string masterKey = "SayraMasterKey2024";

        // 1. Initiate Handshake
        var challengeMsg = authService.InitiateHandshake(clientId);
        Assert.NotNull(challengeMsg.Challenge);

        // 2. Client Side: Generate response
        string clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string dataToSign = $"{challengeMsg.Challenge}{clientNonce}";
        string clientResponse = signatureService.Sign(dataToSign, masterKey);

        var responseMsg = new AuthResponseMessage
        {
            ClientId = clientId,
            Response = clientResponse,
            Nonce = clientNonce
        };

        // 3. Server Side: Authenticate
        var (success, sessionKey) = authService.Authenticate(responseMsg);

        // Assert
        Assert.True(success);
        Assert.NotEmpty(sessionKey);
    }

    [Fact]
    public void Handshake_With_Wrong_Key_Should_Fail()
    {
        // Arrange
        var signatureService = new SignatureService();
        var challengeGenerator = new ChallengeGenerator();
        var authSessionManager = new AuthSessionManager();
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, options);

        string clientId = "PC-01";
        string wrongMasterKey = "WrongKey";

        // 1. Initiate Handshake
        var challengeMsg = authService.InitiateHandshake(clientId);

        // 2. Client Side: Generate response with WRONG key
        string clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string dataToSign = $"{challengeMsg.Challenge}{clientNonce}";
        string clientResponse = signatureService.Sign(dataToSign, wrongMasterKey);

        var responseMsg = new AuthResponseMessage
        {
            ClientId = clientId,
            Response = clientResponse,
            Nonce = clientNonce
        };

        // 3. Server Side: Authenticate
        var (success, sessionKey) = authService.Authenticate(responseMsg);

        // Assert
        Assert.False(success);
        Assert.Empty(sessionKey);
    }

    [Fact]
    public void Replay_Attempt_Should_Fail()
    {
        // Arrange
        var signatureService = new SignatureService();
        var challengeGenerator = new ChallengeGenerator();
        var authSessionManager = new AuthSessionManager();
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, options);
        var replayProtection = new ReplayProtectionService();
        var validator = new SecureMessageValidator(signatureService, replayProtection);

        string clientId = "PC-01";
        string masterKey = "SayraMasterKey2024";

        var challengeMsg = authService.InitiateHandshake(clientId);
        string clientNonce = "nonce123";
        string dataToSign = $"{challengeMsg.Challenge}{clientNonce}";
        string clientResponse = signatureService.Sign(dataToSign, masterKey);

        var responseMsg = new AuthResponseMessage { ClientId = clientId, Response = clientResponse, Nonce = clientNonce };
        var (success, sessionKey) = authService.Authenticate(responseMsg);
        Assert.True(success);

        // Act & Assert
        var envelope = new SecureEnvelope
        {
            ClientId = clientId,
            Nonce = "nonce123",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Payload = "EncryptedData",
            Signature = signatureService.Sign($"EncryptedData:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:nonce123:{clientId}", sessionKey)
        };

        // First use is valid
        Assert.True(validator.Validate(envelope, sessionKey));

        // Second use (replay) should fail
        Assert.False(validator.Validate(envelope, sessionKey));
    }
}
