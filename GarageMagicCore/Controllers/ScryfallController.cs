using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GarageMagicCore.DTOs.Scryfall;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/scryfall")]
[Authorize]
public class ScryfallController : ControllerBase
{
    private readonly IScryfallService _scryfall;

    public ScryfallController(IScryfallService scryfall)
    {
        _scryfall = scryfall;
    }

    /// <summary>
    /// GET /api/scryfall/autocomplete?q={query}&amp;limit={limit}
    /// Returns matching commander name suggestions from Scryfall.
    /// Results are cached server-side for 24 hours.
    /// </summary>
    /// <param name="q">Search query (minimum 2 characters).</param>
    /// <param name="limit">Maximum number of suggestions to return (default 20, max 20).</param>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(CommanderAutocompleteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters." });

        limit = Math.Clamp(limit, 1, 20);
        var result = await _scryfall.AutocompleteCommanderAsync(q, limit);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/scryfall/card?name={name}
    /// Returns full card data for the named commander (fuzzy match).
    /// Use this to retrieve the image URI and colour identity when saving a deck.
    /// Results are cached server-side for 24 hours.
    /// </summary>
    /// <param name="name">Exact or close commander name.</param>
    [HttpGet("card")]
    [ProducesResponseType(typeof(CommanderCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCard([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "name is required." });

        var card = await _scryfall.LookupCommanderAsync(name);
        return card == null
            ? NotFound(new { error = $"No card found matching '{name}'." })
            : Ok(card);
    }

    /// <summary>
    /// GET /api/scryfall/symbology
    /// Returns the full Scryfall mana symbol list, each with an SVG URI.
    /// Use this to render mana costs client-side (e.g. parse "{3}{W}{U}" into symbol images).
    /// Results are cached server-side for 7 days.
    /// </summary>
    [HttpGet("symbology")]
    [ProducesResponseType(typeof(SymbologyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSymbology()
    {
        var result = await _scryfall.GetSymbologyAsync();
        return Ok(result);
    }
}

