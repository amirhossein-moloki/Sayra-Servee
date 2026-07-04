namespace Sayra.Server.Shared.Messages;

public class ClientDisconnectedMessage : BaseMessage
{
    public string Reason { get; set; } = string.Empty;

    public ClientDisconnectedMessage()
    {
        Type = "CLIENT_DISCONNECTED";
    }
}
