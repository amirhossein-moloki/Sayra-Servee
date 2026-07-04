namespace Sayra.Server.Shared.Messages;

public class HeartbeatMessage : BaseMessage
{
    public HeartbeatMessage()
    {
        Type = "HEARTBEAT";
    }
}
