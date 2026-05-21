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
    /// <summary>Role assigned at game start</summary>
    public HiddenRole? HiddenRole { get; set; }
    /// <summary>Role held at game end — differs from HiddenRole when Matriarch swap occurs</summary>
    public HiddenRole? FinalRole { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Match Match { get; set; } = null!;
    public User User { get; set; } = null!;
    public Deck? Deck { get; set; }
}

public enum HiddenRole
{
    Sheriff,    // White  - must survive; Deputy wins if Sheriff wins
    Deputy,     // Blue   - wins with Sheriff even if dead
    Outlaw,     // Red    - 2 players; win if Sheriff dies
    Renegade,   // Black  - 6-player only; wins only when everyone else is dead
    Matriarch   // Purple - wins by delivering killing blow to Sheriff, then winning as new Sheriff
}
