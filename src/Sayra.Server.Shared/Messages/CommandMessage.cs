namespace Sayra.Server.Shared.Messages;

public class CommandMessage : BaseMessage
{
    public string CommandName { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;

    public CommandMessage()
    {
        Type = "COMMAND";
    }
}
