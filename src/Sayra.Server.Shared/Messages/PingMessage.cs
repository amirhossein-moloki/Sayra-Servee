namespace Sayra.Server.Shared.Messages;

public class PingMessage : BaseMessage
{
    public PingMessage()
    {
        Type = "PING";
    }
}
