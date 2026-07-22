using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Application.Messaging;
using Sayra.Server.Authentication;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Network.Tcp;
using Sayra.Server.Security;
using Sayra.Server.Session;
using Sayra.Server.Shared.Messages;
using Xunit;

namespace Sayra.Server.IntegrationTests;

public class TcpProtocolContractTests
{
    private readonly Mock<ILogger<MessageRouter>> _routerLoggerMock = new();
    private readonly Mock<IClientRegistry> _clientRegistryMock = new();
    private readonly Mock<ISessionManager> _sessionManagerMock = new();
    private readonly Mock<ISecureMessageDispatcher> _dispatcherMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly Mock<ITcpConnectionRegistry> _connectionRegistryMock = new();
    private readonly Mock<IAuthSessionManager> _authSessionManagerMock = new();

    private readonly SignatureService _signatureService = new();
    private readonly EncryptionService _encryptionService = new();
    private readonly ReplayProtectionService _replayProtectionService = new();
    private readonly SecureMessageValidator _secureMessageValidator;
    private readonly CommandAuthorizer _authorizer;
    private readonly MessageRouter _router;
    private readonly AuthService _authService;

    public TcpProtocolContractTests()
    {
        _secureMessageValidator = new SecureMessageValidator(_signatureService, _replayProtectionService);
        _authorizer = new CommandAuthorizer(_sessionManagerMock.Object, new Mock<ILogger<CommandAuthorizer>>().Object);
        _router = new MessageRouter(
            _routerLoggerMock.Object,
            _clientRegistryMock.Object,
            _sessionManagerMock.Object,
            _authorizer,
            _dispatcherMock.Object,
            _eventPublisherMock.Object);

        var challengeGenerator = new ChallengeGenerator();
        var options = Microsoft.Extensions.Options.Options.Create(new SecurityOptions { MasterKey = "SayraMasterKey2024" });
        _authService = new AuthService(challengeGenerator, _authSessionManagerMock.Object, _signatureService, _eventPublisherMock.Object, options);
    }

    [Fact]
    public void Test_Serialization_Casing_CamelCase_And_Casing_Match()
    {
        // Arrange
        var telemetry = new TelemetryReportMessage
        {
            ClientId = "PC-01",
            Cpu = 45.5,
            Ram = 2048,
            Uptime = 3600
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(telemetry, options);

        // Assert
        Assert.Contains("\"cpu\":45.5", json);
        Assert.Contains("\"ram\":2048", json);
        Assert.Contains("\"uptime\":3600", json);
        Assert.Contains("\"type\":\"TELEMETRY_REPORT\"", json);
    }

    [Fact]
    public void Test_Polymorphic_Deserialization_For_New_Messages()
    {
        // Arrange
        var telemetryJson = "{\"type\":\"TELEMETRY_REPORT\",\"clientId\":\"PC-01\",\"cpu\":45.5,\"ram\":2048,\"uptime\":3600}";
        var launchedJson = "{\"type\":\"PROCESS_LAUNCHED\",\"clientId\":\"PC-01\",\"gameId\":\"GAME-GTA5\",\"pid\":5120}";
        var exitedJson = "{\"type\":\"PROCESS_EXITED\",\"clientId\":\"PC-01\",\"gameId\":\"GAME-GTA5\",\"exitCode\":0,\"durationSeconds\":3600.0}";
        var resultJson = "{\"type\":\"EXECUTION_RESULT\",\"clientId\":\"PC-01\",\"commandId\":\"CMD-9021\",\"pcId\":\"PC-01\",\"action\":\"LOCK_PC\",\"status\":\"Executed\",\"result\":\"Success\"}";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act & Assert
        var telemetry = JsonSerializer.Deserialize<BaseMessage>(telemetryJson, options);
        Assert.IsType<TelemetryReportMessage>(telemetry);
        Assert.Equal("TELEMETRY_REPORT", telemetry?.Type);

        var launched = JsonSerializer.Deserialize<BaseMessage>(launchedJson, options);
        Assert.IsType<ProcessLaunchedMessage>(launched);
        Assert.Equal("PROCESS_LAUNCHED", launched?.Type);

        var exited = JsonSerializer.Deserialize<BaseMessage>(exitedJson, options);
        Assert.IsType<ProcessExitedMessage>(exited);
        Assert.Equal("PROCESS_EXITED", exited?.Type);

        var result = JsonSerializer.Deserialize<BaseMessage>(resultJson, options);
        Assert.IsType<ExecutionResultMessage>(result);
        Assert.Equal("EXECUTION_RESULT", result?.Type);
    }

    [Fact]
    public async Task Test_Router_Telemetry_Report_Valid_Succeeds()
    {
        // Arrange
        var rawJson = "{\"type\":\"TELEMETRY_REPORT\",\"clientId\":\"PC-01\",\"cpu\":45.5,\"ram\":2048,\"uptime\":3600}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.Is<TelemetryReceivedEvent>(e =>
            e.PcId == "PC-01" &&
            e.CpuUsage == 45.5f &&
            e.RamUsage == 2048f &&
            e.Uptime == 3600
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_Router_Telemetry_Report_Invalid_Cpu_Is_Rejected()
    {
        // Arrange
        var rawJson = "{\"type\":\"TELEMETRY_REPORT\",\"clientId\":\"PC-01\",\"cpu\":120.0,\"ram\":2048,\"uptime\":3600}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<TelemetryReceivedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Test_Router_Process_Launched_Valid_Succeeds()
    {
        // Arrange
        var rawJson = "{\"type\":\"PROCESS_LAUNCHED\",\"clientId\":\"PC-01\",\"gameId\":\"GAME-GTA5\",\"pid\":5120}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.Is<GameStartedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.GameId == "GAME-GTA5" &&
            e.Pid == 5120
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_Router_Process_Launched_Invalid_Pid_Is_Rejected()
    {
        // Arrange
        var rawJson = "{\"type\":\"PROCESS_LAUNCHED\",\"clientId\":\"PC-01\",\"gameId\":\"GAME-GTA5\",\"pid\":-5}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<GameStartedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Test_Router_Process_Exited_Valid_Succeeds()
    {
        // Arrange
        var rawJson = "{\"type\":\"PROCESS_EXITED\",\"clientId\":\"PC-01\",\"gameId\":\"GAME-GTA5\",\"exitCode\":0,\"durationSeconds\":3600.0}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.Is<GameExitedEvent>(e =>
            e.ClientId == "PC-01" &&
            e.GameId == "GAME-GTA5" &&
            e.ExitCode == 0 &&
            e.Duration == "01:00:00"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_Router_Execution_Result_Valid_Succeeds()
    {
        // Arrange
        var rawJson = "{\"type\":\"EXECUTION_RESULT\",\"clientId\":\"PC-01\",\"commandId\":\"CMD-9021\",\"pcId\":\"PC-01\",\"action\":\"LOCK_PC\",\"status\":\"Executed\",\"result\":\"Success\"}";

        // Act
        await _router.RouteAsync(rawJson);

        // Assert
        _eventPublisherMock.Verify(p => p.PublishAsync(It.Is<CommandExecutedEvent>(e =>
            e.CommandId == "CMD-9021" &&
            e.PcId == "PC-01" &&
            e.Action == "LOCK_PC" &&
            e.Result == "Success"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Test_Secure_Envelope_Validation_Flow()
    {
        // Arrange
        string sessionKey = "TestSessionKey123456789012345678";
        string payload = "{\"type\":\"HEARTBEAT\",\"clientId\":\"PC-01\"}";
        string encryptedPayload = _encryptionService.Encrypt(payload, sessionKey);
        var timestamp = DateTime.UtcNow;

        var envelope = new SecureEnvelope
        {
            Payload = encryptedPayload,
            Timestamp = timestamp,
            Signature = _signatureService.Sign($"{encryptedPayload}:{timestamp:O}", sessionKey)
        };

        // Act
        bool isValid = _secureMessageValidator.Validate(envelope, sessionKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Test_Secure_Envelope_With_Stale_Timestamp_Fails()
    {
        // Arrange
        string sessionKey = "TestSessionKey123456789012345678";
        string payload = "{\"type\":\"HEARTBEAT\",\"clientId\":\"PC-01\"}";
        string encryptedPayload = _encryptionService.Encrypt(payload, sessionKey);
        var staleTimestamp = DateTime.UtcNow.AddSeconds(-20); // Drift exceeds 10 seconds limit

        var envelope = new SecureEnvelope
        {
            Payload = encryptedPayload,
            Timestamp = staleTimestamp,
            Signature = _signatureService.Sign($"{encryptedPayload}:{staleTimestamp:O}", sessionKey)
        };

        // Act
        bool isValid = _secureMessageValidator.Validate(envelope, sessionKey);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Test_Secure_Envelope_With_Invalid_Signature_Fails()
    {
        // Arrange
        string sessionKey = "TestSessionKey123456789012345678";
        string payload = "{\"type\":\"HEARTBEAT\",\"clientId\":\"PC-01\"}";
        string encryptedPayload = _encryptionService.Encrypt(payload, sessionKey);
        var timestamp = DateTime.UtcNow;

        var envelope = new SecureEnvelope
        {
            Payload = encryptedPayload,
            Timestamp = timestamp,
            Signature = "InvalidSignatureHex"
        };

        // Act
        bool isValid = _secureMessageValidator.Validate(envelope, sessionKey);

        // Assert
        Assert.False(isValid);
    }
}
