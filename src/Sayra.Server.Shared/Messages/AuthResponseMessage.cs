namespace Sayra.Server.Shared.Messages;

public class AuthResponseMessage : BaseMessage
{
    public string Response { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;

    public AuthResponseMessage()
    {
        Type = "AUTH_RESPONSE";
    }
}
