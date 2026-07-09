using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayra.Server.Configuration.Models;
using Sayra.Server.Discovery.Messages;
using Sayra.Server.Discovery.Services;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Security;
using Moq;
using Xunit;

namespace Sayra.Server.Discovery.Tests;

public class DiscoveryListenerServiceTests
{
    private readonly Mock<ILogger<DiscoveryListenerService>> _loggerMock = new();
    private readonly Mock<IServerIdentityService> _identityServiceMock = new();
    private readonly Mock<IReplayProtectionService> _replayProtectionMock = new();
    private readonly IOptions<SayraConfig> _config;

    public DiscoveryListenerServiceTests()
    {
        var sayraConfig = new SayraConfig
        {
            Discovery = new DiscoveryConfig
            {
                Enabled = true,
                UdpPort = 37020,
                ServerName = "TestServer"
            }
        };
        _config = Options.Create(sayraConfig);
    }

    [Fact]
    public async Task CreateResponseAsync_ShouldReturnSignedResponse()
    {
        // Arrange
        var identity = new ServerIdentityEntity
        {
            Id = "server-id",
            PrivateKey = "private-key",
            PublicKey = "public-key"
        };
        _identityServiceMock.Setup(s => s.GetOrCreateIdentityAsync()).ReturnsAsync(identity);
        _identityServiceMock.Setup(s => s.SignData(It.IsAny<string>(), It.IsAny<string>())).Returns("signed-signature");

        var service = new DiscoveryListenerService(
            _loggerMock.Object,
            _identityServiceMock.Object,
            _replayProtectionMock.Object,
            _config);

        // Act
        // Accessing private method for testing purpose or making it internal
        var method = typeof(DiscoveryListenerService).GetMethod("CreateResponseAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var responseTask = (Task<DiscoveryResponse>)method.Invoke(service, new object[] { "test-nonce" });
        var response = await responseTask;

        // Assert
        Assert.Equal("SAYRA_SERVER_RESPONSE", response.Type);
        Assert.Equal("server-id", response.ServerId);
        Assert.Equal("TestServer", response.ServerName);
        Assert.Equal("test-nonce", response.Nonce);
        Assert.Equal("signed-signature", response.Signature);
    }

    [Fact]
    public void ValidateRequest_ShouldReturnTrue_WhenValid()
    {
        // Arrange
        _replayProtectionMock.Setup(r => r.IsValid(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(true);
        var service = new DiscoveryListenerService(
            _loggerMock.Object,
            _identityServiceMock.Object,
            _replayProtectionMock.Object,
            _config);

        var request = new DiscoveryRequest
        {
            Nonce = "unique-nonce",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        // Act
        var method = typeof(DiscoveryListenerService).GetMethod("ValidateRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isValid = (bool)method.Invoke(service, new object[] { request, endPoint });

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateRequest_ShouldReturnFalse_WhenReplayDetected()
    {
        // Arrange
        _replayProtectionMock.Setup(r => r.IsValid(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(false);
        var service = new DiscoveryListenerService(
            _loggerMock.Object,
            _identityServiceMock.Object,
            _replayProtectionMock.Object,
            _config);

        var request = new DiscoveryRequest
        {
            Nonce = "replayed-nonce",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        // Act
        var method = typeof(DiscoveryListenerService).GetMethod("ValidateRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isValid = (bool)method.Invoke(service, new object[] { request, endPoint });

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateRequest_ShouldReturnFalse_WhenRateLimited()
    {
        // Arrange
        _replayProtectionMock.Setup(r => r.IsValid(It.IsAny<string>(), It.IsAny<DateTime>())).Returns(true);
        var service = new DiscoveryListenerService(
            _loggerMock.Object,
            _identityServiceMock.Object,
            _replayProtectionMock.Object,
            _config);

        var request = new DiscoveryRequest
        {
            Nonce = "nonce1",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        var method = typeof(DiscoveryListenerService).GetMethod("ValidateRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var firstResult = (bool)method.Invoke(service, new object[] { request, endPoint });
        var secondRequest = new DiscoveryRequest { Nonce = "nonce2", Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        var secondResult = (bool)method.Invoke(service, new object[] { secondRequest, endPoint });

        // Assert
        Assert.True(firstResult);
        Assert.False(secondResult);
    }
}
