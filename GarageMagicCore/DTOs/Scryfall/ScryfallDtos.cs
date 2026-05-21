namespace GarageMagicCore.DTOs.Scryfall;

/// <summary>
/// Autocomplete response — a list of matching commander names.
/// </summary>
public class CommanderAutocompleteDto
{
    public List<string> Names { get; set; } = new();
    public int TotalValues { get; set; }
}

/// <summary>
/// Summary of a single Scryfall card — returned by the lookup endpoint.
/// </summary>
public class CommanderCardDto
{
    public required string ScryfallId { get; set; }
    public required string Name { get; set; }
    /// <summary>Normal-size card image URI (front face).</summary>
    public string? ImageUri { get; set; }
    /// <summary>Mana cost string, e.g. "{3}{W}{U}".</summary>
    public string? ManaCost { get; set; }
    /// <summary>Colour identity letters, e.g. ["W","U"].</summary>
    public List<string> ColorIdentity { get; set; } = new();
    public string? TypeLine { get; set; }
    public string? OracleText { get; set; }
}

