namespace Sayra.Server.Shared.Messages;

public class AuthChallengeMessage : BaseMessage
{
    public string Challenge { get; set; } = string.Empty;

    public AuthChallengeMessage()
    {
        Type = "AUTH_CHALLENGE";
    }
}
