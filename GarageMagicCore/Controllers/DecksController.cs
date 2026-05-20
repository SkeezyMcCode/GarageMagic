using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using GarageMagicCore.DTOs.Deck;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/decks")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;
    private readonly IValidator<CreateDeckDto> _createValidator;
    private readonly IValidator<UpdateDeckDto> _updateValidator;

    public DecksController(
        IDeckService deckService,
        IValidator<CreateDeckDto> createValidator,
        IValidator<UpdateDeckDto> updateValidator)
    {
        _deckService = deckService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>POST /api/decks?userId={userId} - Create a deck for a user</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeckDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromQuery] int userId, [FromBody] CreateDeckDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var deck = await _deckService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = deck.Id }, deck);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/decks/{id} - Get deck by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var deck = await _deckService.GetByIdAsync(id);
        return deck == null ? NotFound() : Ok(deck);
    }

    /// <summary>PUT /api/decks/{id} - Update deck</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeckDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var deck = await _deckService.UpdateAsync(id, dto);
        return deck == null ? NotFound() : Ok(deck);
    }

    /// <summary>DELETE /api/decks/{id} - Soft-delete deck</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _deckService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>GET /api/decks/user/{userId} - Get all decks for a user</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<DeckDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var decks = await _deckService.GetByUserAsync(userId);
        return Ok(decks);
    }

    /// <summary>GET /api/decks/{id}/stats - Get deck with performance stats</summary>
    [HttpGet("{id:int}/stats")]
    [ProducesResponseType(typeof(DeckWithStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithStats(int id)
    {
        var deck = await _deckService.GetWithStatsAsync(id);
        return deck == null ? NotFound() : Ok(deck);
    }
}

