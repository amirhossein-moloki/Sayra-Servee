using Xunit;
using Sayra.Server.Authentication;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;
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
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, options);

        string masterKey = "SayraMasterKey2024";
        int clientCount = 200;

        // Act
        var tasks = Enumerable.Range(0, clientCount).Select(async i =>
        {
            string clientId = $"PC-{i:D3}";

            // Handshake
            var challengeMsg = authService.InitiateHandshake(clientId);

            // Response
            string clientNonce = Guid.NewGuid().ToString();
            string dataToSign = $"{challengeMsg.Challenge}{clientNonce}";
            string clientResponse = signatureService.Sign(dataToSign, masterKey);

            var responseMsg = new AuthResponseMessage
            {
                ClientId = clientId,
                Response = clientResponse,
                Nonce = clientNonce
            };

            var (success, sessionKey) = authService.Authenticate(responseMsg);

            Assert.True(success);
            Assert.NotEmpty(sessionKey);
            await Task.Yield();
        });

        await Task.WhenAll(tasks);
    }
}
