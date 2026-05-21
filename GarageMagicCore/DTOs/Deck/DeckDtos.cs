namespace GarageMagicCore.DTOs.Deck;

/// <summary>
/// DTO for creating a new deck
/// </summary>
public class CreateDeckDto
{
    public required string DeckName { get; set; }
    public required string CommanderName { get; set; }
    public string? ColorIdentity { get; set; }
}

/// <summary>
/// DTO for updating a deck
/// </summary>
public class UpdateDeckDto
{
    public string? DeckName { get; set; }
    public string? CommanderName { get; set; }
    public string? ColorIdentity { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for deck response
/// </summary>
public class DeckDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string DeckName { get; set; }
    public required string CommanderName { get; set; }
    public string? ColorIdentity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Scryfall card image URI (normal size). Null if lookup failed or not yet run.</summary>
    public string? CommanderImageUri { get; set; }
    /// <summary>Scryfall card ID.</summary>
    public string? ScryfallId { get; set; }
}

/// <summary>
/// DTO for deck with performance stats
/// </summary>
public class DeckWithStatsDto : DeckDto
{
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
}
