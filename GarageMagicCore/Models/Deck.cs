namespace GarageMagicCore.Models;

public class Deck
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string DeckName { get; set; }
    public required string CommanderName { get; set; }
    public string? ColorIdentity { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Scryfall card image URI (normal size), auto-populated on save.</summary>
    public string? CommanderImageUri { get; set; }

    /// <summary>Scryfall card ID, auto-populated on save.</summary>
    public string? ScryfallId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
