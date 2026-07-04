namespace Sayra.Server.Shared.Messages;

public class AuthMessage : BaseMessage
{
    public string Token { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;

    public AuthMessage()
    {
        Type = "AUTH";
    }
}
