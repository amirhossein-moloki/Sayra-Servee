using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Authentication;
using Sayra.Server.Security;

namespace Sayra.Server.Network.Tcp;

public class TcpServer
{
    private readonly ILogger<TcpServer> _logger;
    private readonly IMessageRouter _messageRouter;
    private readonly IAuthService _authService;
    private readonly ISecureMessageValidator _secureMessageValidator;
    private readonly ISignatureService _signatureService;
    private readonly IEncryptionService _encryptionService;
    private readonly Sayra.Server.Session.ISessionManager _sessionManager;
    private readonly int _port;
    private Socket? _listener;

    public TcpServer(
        ILogger<TcpServer> logger,
        IMessageRouter messageRouter,
        IAuthService authService,
        ISecureMessageValidator secureMessageValidator,
        ISignatureService signatureService,
        IEncryptionService encryptionService,
        Sayra.Server.Session.ISessionManager sessionManager,
        int port = 5000)
    {
        _logger = logger;
        _messageRouter = messageRouter;
        _authService = authService;
        _secureMessageValidator = secureMessageValidator;
        _signatureService = signatureService;
        _encryptionService = encryptionService;
        _sessionManager = sessionManager;
        _port = port;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listener.Listen(100);

        _logger.LogInformation("TCP Server started on port {Port}", _port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var clientSocket = await _listener.AcceptAsync(cancellationToken);
                _logger.LogInformation("New client connected: {RemoteEndPoint}", clientSocket.RemoteEndPoint);

                _ = HandleClientAsync(clientSocket);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TCP Server stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "TCP Server encountered a fatal error");
        }
        finally
        {
            _listener.Close();
        }
    }

    private async Task HandleClientAsync(Socket socket)
    {
        var connection = new ClientConnection(
            socket,
            _logger,
            _messageRouter,
            _authService,
            _secureMessageValidator,
            _signatureService,
            _encryptionService,
            _sessionManager);
        await connection.ProcessAsync();
    }
}
