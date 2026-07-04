using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayra.Server.Authentication;
using Sayra.Server.EventBus;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Sayra.Server.IntegrationTests;

public class Phase3ValidationTests
{
    [Fact]
    public async Task ReconnectStorm_ShouldBeStable()
    {
        // Arrange
        var signatureService = new SignatureService();
        var challengeGenerator = new ChallengeGenerator();
        var authSessionManager = new AuthSessionManager();
        var eventBus = new InMemoryEventBus();
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions());
        var authService = new AuthService(challengeGenerator, authSessionManager, signatureService, eventBus, options);

        string masterKey = "SayraMasterKey2024";
        int clientCount = 500;
        var authenticatedEventCount = 0;

        eventBus.Subscribe<Sayra.Server.EventBus.Events.ClientAuthenticatedEvent>((e, ct) => {
            Interlocked.Increment(ref authenticatedEventCount);
            return Task.CompletedTask;
        });

        // Act
        var tasks = Enumerable.Range(0, clientCount).Select(async i =>
        {
            string clientId = $"PC-{i:D3}";

            // 1. Handshake
            var challengeMsg = authService.InitiateHandshake(clientId);

            // 2. Client Response
            string clientNonce = Guid.NewGuid().ToString();
            string dataToSign = $"{challengeMsg.Challenge}{clientNonce}";
            string clientResponse = signatureService.Sign(dataToSign, masterKey);

            var responseMsg = new AuthResponseMessage
            {
                ClientId = clientId,
                Response = clientResponse,
                Nonce = clientNonce
            };

            // 3. Authenticate
            var (success, sessionKey) = authService.Authenticate(responseMsg);

            Assert.True(success);
            Assert.NotEmpty(sessionKey);
            await Task.Yield();
        });

        await Task.WhenAll(tasks);

        // Assert: Give some time for background events to process
        await Task.Delay(2000);
        Assert.Equal(clientCount, authenticatedEventCount);
    }
}
