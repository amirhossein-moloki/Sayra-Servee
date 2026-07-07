namespace SayraDashboard.Models;

public enum GameButtonState
{
    Play,
    Continue,
    Unavailable
}

public class GameModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public GameButtonState ButtonState { get; set; }
}
