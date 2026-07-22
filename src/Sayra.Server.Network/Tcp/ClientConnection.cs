using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Authentication;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Network.Tcp;

public class ClientConnection
{
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly IMessageRouter _messageRouter;
    private readonly IAuthService _authService;
    private readonly ISecureMessageValidator _secureMessageValidator;
    private readonly ISignatureService _signatureService;
    private readonly IEncryptionService _encryptionService;
    private readonly Sayra.Server.Session.ISessionManager _sessionManager;
    private readonly ITcpConnectionRegistry _connectionRegistry;
    private readonly CancellationTokenSource _cts = new();

    private bool _isAuthenticated = false;
    private string? _clientId;
    private string? _sessionKey;

    public string RemoteEndPoint => _socket.RemoteEndPoint?.ToString() ?? "Unknown";

    public ClientConnection(
        Socket socket,
        ILogger logger,
        IMessageRouter messageRouter,
        IAuthService authService,
        ISecureMessageValidator secureMessageValidator,
        ISignatureService signatureService,
        IEncryptionService encryptionService,
        Sayra.Server.Session.ISessionManager sessionManager,
        ITcpConnectionRegistry connectionRegistry)
    {
        _socket = socket;
        _logger = logger;
        _messageRouter = messageRouter;
        _authService = authService;
        _secureMessageValidator = secureMessageValidator;
        _signatureService = signatureService;
        _encryptionService = encryptionService;
        _sessionManager = sessionManager;
        _connectionRegistry = connectionRegistry;
    }

    public async Task ProcessAsync()
    {
        var pipe = new Pipe();
        var writing = FillPipeAsync(_socket, pipe.Writer);
        var reading = ReadPipeAsync(pipe.Reader);

        await Task.WhenAll(reading, writing);
        if (_clientId != null)
        {
            _connectionRegistry.Unregister(_clientId);
        }
        _logger.LogInformation("Connection closed for {EndPoint}", RemoteEndPoint);
    }

    private async Task FillPipeAsync(Socket socket, PipeWriter writer)
    {
        const int minimumBufferSize = 512;

        while (!_cts.Token.IsCancellationRequested)
        {
            Memory<byte> memory = writer.GetMemory(minimumBufferSize);
            try
            {
                int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, _cts.Token);
                if (bytesRead == 0)
                {
                    break;
                }
                writer.Advance(bytesRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving from {EndPoint}", RemoteEndPoint);
                break;
            }

            FlushResult result = await writer.FlushAsync(_cts.Token);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync();
    }

    private async Task ReadPipeAsync(PipeReader reader)
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(_cts.Token);
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
            {
                var rawMessage = Encoding.UTF8.GetString(line);
                await HandleMessageAsync(rawMessage);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }

    private async Task HandleMessageAsync(string rawMessage)
    {
        if (!_isAuthenticated)
        {
            await HandlePreAuthMessageAsync(rawMessage);
        }
        else
        {
            await HandlePostAuthMessageAsync(rawMessage);
        }
    }

    private async Task HandlePreAuthMessageAsync(string rawMessage)
    {
        try
        {
            var baseMsg = JsonSerializer.Deserialize<BaseMessage>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (baseMsg == null) return;

            if (baseMsg.Type == "AUTH")
            {
                var authMsg = JsonSerializer.Deserialize<AuthMessage>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _clientId = baseMsg.ClientId;

                // Update Client Registry with identity data
                var client = _messageRouter.GetClient(_clientId) ?? new Sayra.Server.Domain.Entities.Client { Id = _clientId };
                if (authMsg != null)
                {
                    client.MacAddress = authMsg.MacAddress;
                    client.Hostname = authMsg.Hostname;
                }
                client.IPAddress = _socket.RemoteEndPoint?.ToString() ?? "Unknown";
                _messageRouter.UpdateClient(client);

                var challenge = _authService.InitiateHandshake(_clientId);
                await SendMessageAsync(challenge);
            }
            else if (baseMsg.Type == "AUTH_RESPONSE")
            {
                var response = JsonSerializer.Deserialize<AuthResponseMessage>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (response == null) return;

                var (success, sessionKey) = _authService.Authenticate(response);
                if (success)
                {
                    _isAuthenticated = true;
                    _sessionKey = sessionKey;
                    _sessionManager.CreateSession(_clientId!, _sessionKey);
                    _connectionRegistry.Register(_clientId!, this);
                    var sessionData = _sessionManager.GetSession(_clientId!);
                    await SendMessageAsync(new AuthStatusMessage
                    {
                        Status = "SUCCESS",
                        Message = "Authenticated",
                        ClientId = _clientId!
                    });
                    _logger.LogInformation("Client {ClientId} authenticated successfully and session created", _clientId);
                }
                else
                {
                    await SendMessageAsync(new AuthStatusMessage { Status = "FAILED", Message = "Authentication failed", ClientId = _clientId ?? "Unknown" });
                    _logger.LogWarning("Authentication failed for {EndPoint}", RemoteEndPoint);
                    Disconnect();
                }
            }
            else
            {
                _logger.LogWarning("Unauthorized message type {Type} from {EndPoint}", baseMsg.Type, RemoteEndPoint);
                Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pre-auth message handling");
            Disconnect();
        }
    }

    private async Task HandlePostAuthMessageAsync(string rawMessage)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<SecureEnvelope>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (envelope == null) return;

            if (_secureMessageValidator.Validate(envelope, _sessionKey!))
            {
                var decryptedPayload = _encryptionService.Decrypt(envelope.Payload, _sessionKey!);
                await _messageRouter.RouteAsync(decryptedPayload);
            }
            else
            {
                _logger.LogWarning("Invalid secure envelope from {ClientId}", _clientId);
                // Drop packet silently as per requirement, but logging for visibility
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling post-auth message from {ClientId}", _clientId);
        }
    }

    public async Task SendMessageAsync<T>(T message)
    {
        string json;
        if (_isAuthenticated && _sessionKey != null)
        {
            var payloadJson = JsonSerializer.Serialize(message);
            var encryptedPayload = _encryptionService.Encrypt(payloadJson, _sessionKey);
            var timestamp = DateTime.UtcNow;

            var envelope = new SecureEnvelope
            {
                Payload = encryptedPayload,
                Timestamp = timestamp,
                Signature = _signatureService.Sign($"{encryptedPayload}:{timestamp:O}", _sessionKey)
            };
            json = JsonSerializer.Serialize(envelope);
        }
        else
        {
            json = JsonSerializer.Serialize(message);
        }

        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _socket.SendAsync(bytes, SocketFlags.None, _cts.Token);
    }

    private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        SequencePosition? position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    public void Disconnect()
    {
        _cts.Cancel();
        if (_clientId != null)
        {
            _connectionRegistry.Unregister(_clientId);
        }
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch { }
    }
}
