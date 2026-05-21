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

/// <summary>
/// A single mana / card symbol from the Scryfall symbology list.
/// </summary>
public class ManaSymbolDto
{
    /// <summary>Symbol notation, e.g. "{W}", "{2}", "{T}".</summary>
    public required string Symbol { get; set; }
    /// <summary>SVG image URI hosted by Scryfall CDN.</summary>
    public required string SvgUri { get; set; }
    /// <summary>Human-readable description, e.g. "one white mana".</summary>
    public string? Description { get; set; }
    /// <summary>Converted mana cost value.</summary>
    public decimal Cmc { get; set; }
    /// <summary>Whether this symbol appears in mana costs.</summary>
    public bool AppearsInManaCosts { get; set; }
    public List<string> Colors { get; set; } = new();
}

/// <summary>
/// Full symbology list returned by GET /api/scryfall/symbology.
/// </summary>
public class SymbologyDto
{
    public List<ManaSymbolDto> Symbols { get; set; } = new();
}
