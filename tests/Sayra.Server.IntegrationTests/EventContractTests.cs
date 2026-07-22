using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Sayra.Server.Application.Messaging;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Shared.Messages;
using Xunit;

namespace Sayra.Server.IntegrationTests;

public class EventContractTests
{
    private readonly Mock<IEventPublisher> _mockPublisher;
    private readonly MessageRouter _router;

    public EventContractTests()
    {
        _mockPublisher = new Mock<IEventPublisher>();
        var mockLogger = new Mock<ILogger<MessageRouter>>();
        var mockRegistry = new Mock<Sayra.Server.Application.Interfaces.IClientRegistry>();
        var mockSession = new Mock<Sayra.Server.Session.ISessionManager>();
        var mockDispatcher = new Mock<ISecureMessageDispatcher>();

        var authorizerLogger = new Mock<ILogger<CommandAuthorizer>>();
        var authorizer = new CommandAuthorizer(mockSession.Object, authorizerLogger.Object);

        _router = new MessageRouter(
            mockLogger.Object,
            mockRegistry.Object,
            mockSession.Object,
            authorizer,
            mockDispatcher.Object,
            _mockPublisher.Object);
    }

    [Fact]
    public void TestDeserialization()
    {
        var rawJson = "{\"type\":\"AuthenticationStarted\",\"clientId\":\"PC-01\",\"username\":\"gamer123\",\"timestamp\":\"2026-10-18T12:00:05Z\"}";
        var msg = JsonSerializer.Deserialize<BaseMessage>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(msg);
        Assert.Equal("PC-01", msg.ClientId);
        Assert.Equal("AuthenticationStarted", msg.Type);
        Assert.Equal("2026-10-18T12:00:05Z", msg.Timestamp.ToString());
    }

    [Fact]
    public async Task Route_AuthenticationStarted_With_Valid_Payload_Succeeds()
    {
        var rawJson = "{\"type\":\"AuthenticationStarted\",\"clientId\":\"PC-01\",\"username\":\"gamer123\",\"timestamp\":\"2026-10-18T12:00:05Z\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<AuthenticationStartedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.Username == "gamer123" &&
            e.Timestamp == "2026-10-18T12:00:05Z"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_AuthenticationStarted_With_Invalid_Payload_Is_Rejected()
    {
        // Missing username
        var rawJson = "{\"type\":\"AuthenticationStarted\",\"clientId\":\"PC-01\",\"timestamp\":\"2026-10-18T12:00:05Z\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<AuthenticationStartedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Route_AuthenticationSucceeded_With_Valid_Payload_Succeeds()
    {
        var rawJson = "{\"type\":\"AuthenticationSucceeded\",\"clientId\":\"PC-01\",\"user\":{\"id\":\"u-101\",\"username\":\"gamer123\",\"displayName\":\"Gamer One\",\"role\":\"Player\",\"permissions\":[\"PlayGame\"],\"preferredLanguage\":\"fa-IR\",\"preferredTheme\":\"Dark\"},\"authenticationType\":\"Server\",\"sessionId\":\"sess-999\",\"timestamp\":\"2026-10-18T12:00:05Z\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<AuthenticationSucceededEvent>(e =>
            e.ClientId == "PC-01" &&
            e.User.Id == "u-101" &&
            e.User.Role == "Player" &&
            e.AuthenticationType == "Server" &&
            e.SessionId == "sess-999" &&
            e.Timestamp == "2026-10-18T12:00:05Z"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_AuthenticationSucceeded_With_Invalid_Role_Is_Rejected()
    {
        // "SuperUser" is not a valid role (only Player, Admin, Operator are allowed)
        var rawJson = "{\"type\":\"AuthenticationSucceeded\",\"clientId\":\"PC-01\",\"user\":{\"id\":\"u-101\",\"username\":\"gamer123\",\"displayName\":\"Gamer One\",\"role\":\"SuperUser\",\"permissions\":[\"PlayGame\"],\"preferredLanguage\":\"fa-IR\",\"preferredTheme\":\"Dark\"},\"authenticationType\":\"Server\",\"sessionId\":\"sess-999\",\"timestamp\":\"2026-10-18T12:00:05Z\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<AuthenticationSucceededEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Route_LogoutStarted_With_Valid_Payload_Succeeds()
    {
        var rawJson = "{\"type\":\"LogoutStarted\",\"clientId\":\"PC-01\",\"user\":{\"username\":\"gamer123\",\"role\":\"Player\"},\"timestamp\":\"2026-10-18T12:00:05Z\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<LogoutStartedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.User.Username == "gamer123" &&
            e.User.Role == "Player" &&
            e.Timestamp == "2026-10-18T12:00:05Z"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_GameExited_With_Valid_Duration_Succeeds()
    {
        var rawJson = "{\"type\":\"GameExited\",\"clientId\":\"PC-01\",\"gameId\":\"g-55\",\"name\":\"Dota 2\",\"exitCode\":0,\"duration\":\"01:30:00\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<GameExitedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.GameId == "g-55" &&
            e.Name == "Dota 2" &&
            e.ExitCode == 0 &&
            e.Duration == "01:30:00"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_GameExited_With_Invalid_Duration_Is_Rejected()
    {
        // "One Hour" is not a valid TimeSpan format
        var rawJson = "{\"type\":\"GameExited\",\"clientId\":\"PC-01\",\"gameId\":\"g-55\",\"name\":\"Dota 2\",\"exitCode\":0,\"duration\":\"One Hour\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.IsAny<GameExitedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Route_GameCrashed_With_Valid_Payload_Succeeds()
    {
        var rawJson = "{\"type\":\"GameCrashed\",\"clientId\":\"PC-01\",\"gameId\":\"g-55\",\"name\":\"Dota 2\",\"exitCode\":139,\"reason\":\"Segmentation Fault\"}";

        await _router.RouteAsync(rawJson);

        _mockPublisher.Verify(p => p.PublishAsync(It.Is<GameCrashedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.GameId == "g-55" &&
            e.Name == "Dota 2" &&
            e.ExitCode == 139 &&
            e.Reason == "Segmentation Fault"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
