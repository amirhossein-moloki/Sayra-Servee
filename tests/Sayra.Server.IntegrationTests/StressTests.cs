using Xunit;
using Moq;
using Sayra.Server.Authentication;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;
using Sayra.Server.EventBus.Interfaces;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Sayra.Server.IntegrationTests;

public class StressTests
{
    [Fact]
    public async Task Concurrent_Authentication_Should_Succeed()
    {
        // Arrange
        var signatureService = new SignatureService();
        var challengeGenerator = new ChallengeGenerator();
        var authSessionManager = new AuthSessionManager();
        var eventPublisher = new Mock<IEventPublisher>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, eventPublisher, options);

        string masterKey = "SayraMasterKey2024";
        int clientCount = 200;

        // Act
        var tasks = Enumerable.Range(0, clientCount).Select(async i =>
        {
            string clientId = $"PC-{i:D3}";

            // Handshake
            var challengeMsg = authService.InitiateHandshake(clientId);

            // Response
            string clientSessionKey = Guid.NewGuid().ToString();
            string dataToSign = challengeMsg.Challenge;
            string clientResponse = signatureService.Sign(dataToSign, masterKey);

            var responseMsg = new AuthResponseMessage
            {
                ClientId = clientId,
                Response = clientResponse,
                SessionKey = clientSessionKey
            };

            var (success, sessionKey) = authService.Authenticate(responseMsg);

            Assert.True(success);
            Assert.NotEmpty(sessionKey);
            await Task.Yield();
        });

        await Task.WhenAll(tasks);
    }
}
