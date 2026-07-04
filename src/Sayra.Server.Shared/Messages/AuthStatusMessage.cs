namespace Sayra.Server.Shared.Messages;

public class AuthStatusMessage : BaseMessage
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;

    public AuthStatusMessage()
    {
        Type = "AUTH_STATUS";
    }
}
