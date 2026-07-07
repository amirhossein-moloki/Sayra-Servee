using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SayraDashboard.Models;

namespace SayraDashboard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string timerValue = "00:00:00";

    [ObservableProperty]
    private string pcName = "PC-08";

    [ObservableProperty]
    private string phoneCost = "110,000 تومان";

    [ObservableProperty]
    private string walletBalance = "120,000 تومان";

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string currentTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private string currentDate = DateTime.Now.ToString("yyyy/MM/dd");

    public ObservableCollection<GameModel> GameItems { get; } = new();

    public MainViewModel()
    {
        // Sample data
        GameItems.Add(new GameModel { Id = 1, Name = "Counter-Strike 2", Category = "Shooter", ButtonText = "اجرا", ButtonState = GameButtonState.Play });
        GameItems.Add(new GameModel { Id = 2, Name = "Dota 2", Category = "MOBA", ButtonText = "ادامه", ButtonState = GameButtonState.Continue });
        GameItems.Add(new GameModel { Id = 3, Name = "Cyberpunk 2077", Category = "RPG", ButtonText = "ناموجود", ButtonState = GameButtonState.Unavailable });
    }

    [RelayCommand]
    private void PlayGame(GameModel game)
    {
        // Implementation for playing game
    }

    [RelayCommand]
    private void Shutdown()
    {
        // Implementation for shutdown
    }

    [RelayCommand]
    private void EndSession()
    {
        // Implementation for ending session
    }
}
