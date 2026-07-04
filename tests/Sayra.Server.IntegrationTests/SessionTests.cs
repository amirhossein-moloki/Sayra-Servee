using Xunit;
using Sayra.Server.Session;
using Sayra.Server.Domain.Entities;

namespace Sayra.Server.IntegrationTests;

public class SessionTests
{
    [Fact]
    public void SessionManager_Should_Remain_Authoritative()
    {
        // Arrange
        var registry = new SessionRegistry();
        var manager = new SessionManager(registry);
        string clientId = "PC-01";
        string sessionKey = "key123";

        // Act: Create Session
        manager.CreateSession(clientId, sessionKey);
        var initialSession = manager.GetSession(clientId).Session;

        Assert.NotNull(initialSession);
        Assert.True(manager.IsSessionActive(clientId));

        // Simulate Disconnect / Reconnect Logic
        // In our system, the session stays in the registry until explicitly ended or timed out

        // Assert: Same identity reconnects, we still have the session
        var reconnectedSession = manager.GetSession(clientId).Session;
        Assert.Equal(initialSession.Id, reconnectedSession.Id);

        // Act: End session
        manager.EndSession(clientId);

        // Assert: Session is gone
        Assert.False(manager.IsSessionActive(clientId));
        Assert.Null(manager.GetSession(clientId).Session);
    }
}
