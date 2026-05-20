namespace GarageMagicCore.Models;

/// <summary>
/// Tracks all participants in a match with their hidden roles (for Sheriff games)
/// </summary>
public class MatchParticipant
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int UserId { get; set; }
    public int? DeckId { get; set; }
    public HiddenRole? HiddenRole { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
    public Deck? Deck { get; set; }
}

public enum HiddenRole
{
    Sheriff,
    Deputy,
    Red      // Outlaw/Renegade
}

