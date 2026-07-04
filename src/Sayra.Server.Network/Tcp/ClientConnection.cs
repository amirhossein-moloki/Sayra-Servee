using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;

namespace Sayra.Server.Network.Tcp;

public class ClientConnection
{
    private readonly Socket _socket;
    private readonly ILogger _logger;
    private readonly IMessageRouter _messageRouter;
    private readonly CancellationTokenSource _cts = new();

    public string RemoteEndPoint => _socket.RemoteEndPoint?.ToString() ?? "Unknown";

    public ClientConnection(Socket socket, ILogger logger, IMessageRouter messageRouter)
    {
        _socket = socket;
        _logger = logger;
        _messageRouter = messageRouter;
    }

    public async Task ProcessAsync()
    {
        var pipe = new Pipe();
        var writing = FillPipeAsync(_socket, pipe.Writer);
        var reading = ReadPipeAsync(pipe.Reader);

        await Task.WhenAll(reading, writing);
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
                var message = Encoding.UTF8.GetString(line);
                await _messageRouter.RouteAsync(message);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
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
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch { }
    }
}
