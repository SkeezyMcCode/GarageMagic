namespace GarageMagicCore.Models;

/// <summary>
/// Tracks prestige level history per season
/// </summary>
public class PrestigeLevel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SeasonId { get; set; }
    public int Level { get; set; }
    public DateTime AchievedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Season Season { get; set; } = null!;
}

