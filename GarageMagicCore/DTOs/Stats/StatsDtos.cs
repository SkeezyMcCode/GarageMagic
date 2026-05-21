namespace GarageMagicCore.DTOs.Stats;

/// <summary>
/// DTO for user stats within a season
/// </summary>
public class UserStatsDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public int SeasonId { get; set; }
    public required string SeasonName { get; set; }

    // Overall
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalMatches { get; set; }
    public decimal WinRate { get; set; }

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

    // Matriarch stats
    public int MatriarchTriggered { get; set; }
    public int MatriarchWins { get; set; }

    // Prestige
    public int PrestigeLevel { get; set; }
}

/// <summary>
/// DTO for deck stats
/// </summary>
public class DeckStatsDto
{
    public int DeckId { get; set; }
    public required string DeckName { get; set; }
    public required string CommanderName { get; set; }
    public int TotalWins { get; set; }
}

