namespace Sayra.Server.Shared.Messages;

public class ClientConnectedMessage : BaseMessage
{
    public string IPAddress { get; set; } = string.Empty;

    public ClientConnectedMessage()
    {
        Type = "CLIENT_CONNECTED";
    }
}
