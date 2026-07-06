namespace Sayra.Server.Shared.Messages;

public class AuthStatusMessage : BaseMessage
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public AuthStatusMessage()
    {
        Type = "AUTH_STATUS";
    }
}
