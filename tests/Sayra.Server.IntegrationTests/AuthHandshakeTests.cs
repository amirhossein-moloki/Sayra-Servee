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
        var eventPublisher = new Mock<Sayra.Server.EventBus.Interfaces.IEventPublisher>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, eventPublisher, options);

        string clientId = "PC-01";
        string masterKey = "SayraMasterKey2024";

        // 1. Initiate Handshake
        var challengeMsg = authService.InitiateHandshake(clientId);
        Assert.NotNull(challengeMsg.Challenge);

        // 2. Client Side: Generate response
        string clientSessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string dataToSign = challengeMsg.Challenge;
        string clientResponse = signatureService.Sign(dataToSign, masterKey);

        var responseMsg = new AuthResponseMessage
        {
            ClientId = clientId,
            Response = clientResponse,
            SessionKey = clientSessionKey
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
        var eventPublisher = new Mock<Sayra.Server.EventBus.Interfaces.IEventPublisher>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, eventPublisher, options);

        string clientId = "PC-01";
        string wrongMasterKey = "WrongKey";

        // 1. Initiate Handshake
        var challengeMsg = authService.InitiateHandshake(clientId);

        // 2. Client Side: Generate response with WRONG key
        string clientSessionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        string dataToSign = challengeMsg.Challenge;
        string clientResponse = signatureService.Sign(dataToSign, wrongMasterKey);

        var responseMsg = new AuthResponseMessage
        {
            ClientId = clientId,
            Response = clientResponse,
            SessionKey = clientSessionKey
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
        var eventPublisher = new Mock<Sayra.Server.EventBus.Interfaces.IEventPublisher>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, eventPublisher, options);
        var replayProtection = new ReplayProtectionService();
        var validator = new SecureMessageValidator(signatureService, replayProtection);

        string clientId = "PC-01";
        string masterKey = "SayraMasterKey2024";

        var challengeMsg = authService.InitiateHandshake(clientId);
        string clientSessionKey = "dummy-session-key";
        string dataToSign = challengeMsg.Challenge;
        string clientResponse = signatureService.Sign(dataToSign, masterKey);

        var responseMsg = new AuthResponseMessage { ClientId = clientId, Response = clientResponse, SessionKey = clientSessionKey };
        var (success, sessionKey) = authService.Authenticate(responseMsg);
        Assert.True(success);

        // Act & Assert
        var timestamp = DateTime.UtcNow;
        var envelope = new SecureEnvelope
        {
            Timestamp = timestamp,
            Payload = "EncryptedData",
            Signature = signatureService.Sign($"EncryptedData:{timestamp:O}", sessionKey)
        };

        // First use is valid
        Assert.True(validator.Validate(envelope, sessionKey));

        // Replay within 10s should now fail because we track signatures
        Assert.False(validator.Validate(envelope, sessionKey));

        // Replay with old timestamp should fail
        var oldEnvelope = new SecureEnvelope
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            Payload = "EncryptedData",
            Signature = signatureService.Sign($"EncryptedData:{DateTime.UtcNow.AddMinutes(-5):O}", sessionKey)
        };
        Assert.False(validator.Validate(oldEnvelope, sessionKey));
    }
}
