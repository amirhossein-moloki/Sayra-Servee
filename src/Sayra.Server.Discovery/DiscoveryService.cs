using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayra.Server.Configuration.Models;
using Sayra.Server.Security;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Discovery;

public class DiscoveryService : BackgroundService
{
    private readonly ILogger<DiscoveryService> _logger;
    private readonly ISignatureService _signatureService;
    private readonly IReplayProtectionService _replayProtectionService;
    private readonly SayraConfig _config;
    private readonly SecurityOptions _securityOptions;

    public DiscoveryService(
        ILogger<DiscoveryService> logger,
        ISignatureService signatureService,
        IReplayProtectionService replayProtectionService,
        IOptions<SayraConfig> config,
        IOptions<SecurityOptions> securityOptions)
    {
        _logger = logger;
        _signatureService = signatureService;
        _replayProtectionService = replayProtectionService;
        _config = config.Value;
        _securityOptions = securityOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Discovery.Enabled)
        {
            _logger.LogInformation("Discovery service is disabled via configuration.");
            return;
        }

        try
        {
            using var udpClient = new UdpClient(_config.Discovery.UdpPort);
            _logger.LogInformation("Sayra Discovery Service listening on UDP port {Port}", _config.Discovery.UdpPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync(stoppingToken);
                    _logger.LogDebug("Received discovery request from {RemoteEndPoint}", result.RemoteEndPoint);

                    _ = HandleRequestAsync(udpClient, result, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving discovery request");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to start UDP Discovery Listener on port {Port}", _config.Discovery.UdpPort);
        }
    }

    private async Task HandleRequestAsync(UdpClient udpClient, UdpReceiveResult result, CancellationToken stoppingToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(result.Buffer);
            var request = JsonSerializer.Deserialize<BaseMessage>(json);

            if (request == null || request.Type != "DISCOVER_SAYRA_SERVER")
            {
                _logger.LogWarning("Invalid discovery request type from {RemoteEndPoint}", result.RemoteEndPoint);
                return;
            }

            var discoveryRequest = JsonSerializer.Deserialize<DiscoveryRequest>(json);
            if (discoveryRequest == null || string.IsNullOrEmpty(discoveryRequest.Nonce))
            {
                _logger.LogWarning("Invalid discovery request format from {RemoteEndPoint}", result.RemoteEndPoint);
                return;
            }

            // Replay protection (using Nonce as a unique identifier for the request signature in this context)
            if (!_replayProtectionService.IsValid(discoveryRequest.Nonce, DateTimeOffset.FromUnixTimeSeconds(discoveryRequest.Timestamp).UtcDateTime))
            {
                _logger.LogWarning("Discovery request from {RemoteEndPoint} failed replay protection (Nonce: {Nonce})",
                    result.RemoteEndPoint, discoveryRequest.Nonce);
                return;
            }

            _logger.LogInformation("Valid discovery request received from {RemoteEndPoint} (Client: {ClientId})",
                result.RemoteEndPoint, discoveryRequest.ClientId);

            // Prepare response
            var response = new DiscoveryResponse
            {
                ServerId = _config.Discovery.ServerId,
                Ip = GetLocalIpAddress(),
                TcpPort = 5000,
                ApiPort = 7000,
                Version = "1.0.0",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Sign response: HMAC-SHA256(serverId|ip|tcpPort|timestamp)
            string dataToSign = $"{response.ServerId}|{response.Ip}|{response.TcpPort}|{response.Timestamp}";
            response.Signature = _signatureService.Sign(dataToSign, _securityOptions.MasterKey);

            var responseJson = JsonSerializer.Serialize(response);
            var responseBuffer = Encoding.UTF8.GetBytes(responseJson);

            await udpClient.SendAsync(responseBuffer, responseBuffer.Length, result.RemoteEndPoint);
            _logger.LogInformation("Sent discovery response to {RemoteEndPoint}", result.RemoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling discovery request from {RemoteEndPoint}", result.RemoteEndPoint);
        }
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to determine local IP address, falling back to localhost.");
        }
        return "127.0.0.1";
    }
}
