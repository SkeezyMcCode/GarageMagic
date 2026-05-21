namespace GarageMagicCore.Models;

/// <summary>
/// Tracks user statistics per season
/// </summary>
public class UserStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SeasonId { get; set; }
    
    // Overall stats
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalMatches { get; set; }
    
    // Match type breakdown
    public int Wins1v1v1 { get; set; }
    public int Wins1v1v1v1 { get; set; }
    public int WinsSheriff { get; set; }
    
    // Sheriff role stats
    public int SheriffGamesPlayed { get; set; }
    public int SheriffGamesWon { get; set; }
    public int DeputyGamesPlayed { get; set; }
    public int DeputyGamesWon { get; set; }
    public int OutlawGamesPlayed { get; set; }
    public int OutlawGamesWon { get; set; }
    public int RenegadeGamesPlayed { get; set; }
    public int RenegadeGamesWon { get; set; }
    
    // Matriarch stats — triggered when an Outlaw kills the Sheriff and takes over
    public int MatriarchTriggered { get; set; }  // Times this player triggered the swap
    public int MatriarchWins { get; set; }        // Times they won after becoming new Sheriff
    
    // Deck performance (stored as JSON for flexibility)
    public string? WinsPerDeckJson { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
