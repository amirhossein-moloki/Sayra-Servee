using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayra.Server.Configuration.Models;
using Sayra.Server.Discovery.Messages;
using Sayra.Server.Security;

namespace Sayra.Server.Discovery.Services;

public class DiscoveryListenerService : BackgroundService
{
    private readonly ILogger<DiscoveryListenerService> _logger;
    private readonly IServerIdentityService _identityService;
    private readonly IReplayProtectionService _replayProtection;
    private readonly DiscoveryConfig _config;
    private readonly int _tcpPort;
    private readonly int _apiPort;

    private readonly HashSet<string> _rateLimitedClients = new();
    private DateTime _lastRateLimitCleanup = DateTime.UtcNow;

    public DiscoveryListenerService(
        ILogger<DiscoveryListenerService> logger,
        IServerIdentityService identityService,
        IReplayProtectionService replayProtection,
        IOptions<SayraConfig> config,
        int tcpPort = 5000,
        int apiPort = 7000)
    {
        _logger = logger;
        _identityService = identityService;
        _replayProtection = replayProtection;
        _config = config.Value.Discovery;
        _tcpPort = tcpPort;
        _apiPort = apiPort;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Discovery service is disabled.");
            return;
        }

        using var udpClient = new UdpClient(_config.UdpPort);
        _logger.LogInformation("Discovery listener started on UDP port {Port}", _config.UdpPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var requestJson = Encoding.UTF8.GetString(result.Buffer);

                _logger.LogInformation("Discovery request received from {EndPoint}", result.RemoteEndPoint);

                var request = JsonSerializer.Deserialize<DiscoveryRequest>(requestJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null || request.Type != "DISCOVER_SAYRA_SERVER")
                {
                    _logger.LogWarning("Invalid discovery request from {EndPoint}", result.RemoteEndPoint);
                    continue;
                }

                if (!ValidateRequest(request, result.RemoteEndPoint))
                {
                    continue;
                }

                var response = await CreateResponseAsync(request.Nonce);
                var responseJson = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                await udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                _logger.LogInformation("Discovery response sent to {EndPoint}", result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling discovery request");
            }

            CleanupRateLimits();
        }
    }

    private bool ValidateRequest(DiscoveryRequest request, IPEndPoint remoteEndPoint)
    {
        // Replay Protection
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(request.Timestamp).UtcDateTime;
        if (!_replayProtection.IsValid(request.Nonce, timestamp))
        {
            _logger.LogWarning("Discovery replay attack or invalid timestamp from {EndPoint}", remoteEndPoint);
            return false;
        }

        // Rate Limiting (Simple per-IP for UDP)
        var clientKey = remoteEndPoint.Address.ToString();
        lock (_rateLimitedClients)
        {
            if (_rateLimitedClients.Contains(clientKey))
            {
                _logger.LogWarning("Discovery rate limit exceeded for {EndPoint}", remoteEndPoint);
                return false;
            }
            _rateLimitedClients.Add(clientKey);
        }

        return true;
    }

    private async Task<DiscoveryResponse> CreateResponseAsync(string nonce)
    {
        var identity = await _identityService.GetOrCreateIdentityAsync();

        var response = new DiscoveryResponse
        {
            Type = "SAYRA_SERVER_RESPONSE",
            ServerId = identity.Id,
            ServerName = _config.ServerName,
            Ip = GetLocalIpAddress(),
            TcpPort = _tcpPort,
            ApiPort = _apiPort,
            Priority = _config.Priority,
            Version = "1.0.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nonce = nonce
        };

        // Sign: ServerId + ServerName + IP + TCPPort + Timestamp + Nonce
        var dataToSign = $"{response.ServerId}{response.ServerName}{response.Ip}{response.TcpPort}{response.Timestamp}{response.Nonce}";
        response.Signature = _identityService.SignData(dataToSign, identity.PrivateKey);

        return response;
    }

    private string GetLocalIpAddress()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
        }
        catch
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }

    private void CleanupRateLimits()
    {
        if ((DateTime.UtcNow - _lastRateLimitCleanup).TotalSeconds < 10) return;

        lock (_rateLimitedClients)
        {
            _rateLimitedClients.Clear();
        }
        _lastRateLimitCleanup = DateTime.UtcNow;
    }
}
