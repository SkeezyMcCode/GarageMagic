using GarageMagicCore.DTOs.Scryfall;

namespace GarageMagicCore.Services;

public interface IScryfallService
{
    /// <summary>
    /// Returns up to <paramref name="limit"/> commander name suggestions for the given query.
    /// Results are cached for 24 hours.
    /// </summary>
    Task<CommanderAutocompleteDto> AutocompleteCommanderAsync(string query, int limit = 20);

    /// <summary>
    /// Returns full card data for the named commander (fuzzy match).
    /// Returns null if no matching card is found.
    /// Results are cached for 24 hours.
    /// </summary>
    Task<CommanderCardDto?> LookupCommanderAsync(string name);

    /// <summary>
    /// Returns the full Scryfall symbology list (all mana symbols with SVG URIs).
    /// Results are cached for 7 days — the list almost never changes.
    /// </summary>
    Task<SymbologyDto> GetSymbologyAsync();
}

