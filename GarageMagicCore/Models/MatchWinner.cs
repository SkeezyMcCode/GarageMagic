namespace GarageMagicCore.Models;

/// <summary>
/// Junction table for Match-User many-to-many relationship (for multiple winners in Sheriff games)
/// </summary>
public class MatchWinner
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
}

