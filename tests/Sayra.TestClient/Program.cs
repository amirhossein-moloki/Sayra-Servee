using System.Net.Sockets;
using System.Text;

Console.WriteLine("Connecting to Sayra Server...");

try
{
    using var client = new TcpClient("localhost", 5000);
    using var stream = client.GetStream();
    Console.WriteLine("Connected!");

    async Task SendMessage(string message)
    {
        var data = Encoding.UTF8.GetBytes(message + "\n");
        await stream.WriteAsync(data, 0, data.Length);
        Console.WriteLine($"Sent: {message}");
    }

    await SendMessage("{\"type\":\"CLIENT_CONNECTED\",\"clientId\":\"PC-01\",\"ipAddress\":\"127.0.0.1\"}");
    await Task.Delay(1000);
    await SendMessage("{\"type\":\"HEARTBEAT\",\"clientId\":\"PC-01\"}");
    await Task.Delay(1000);
    await SendMessage("{\"type\":\"PING\",\"clientId\":\"PC-01\"}");
    await Task.Delay(1000);
    await SendMessage("{\"type\":\"CLIENT_DISCONNECTED\",\"clientId\":\"PC-01\",\"reason\":\"User logout\"}");

    Console.WriteLine("Test completed.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
