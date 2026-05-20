using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using GarageMagicCore.DTOs.Match;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;
    private readonly IValidator<CreateMatchDto> _createValidator;

    public MatchesController(IMatchService matchService, IValidator<CreateMatchDto> createValidator)
    {
        _matchService = matchService;
        _createValidator = createValidator;
    }

    /// <summary>POST /api/matches - Record a new match</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMatchDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var match = await _matchService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = match.Id }, match);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/matches/{id} - Get match by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var match = await _matchService.GetByIdAsync(id);
        return match == null ? NotFound() : Ok(match);
    }

    /// <summary>DELETE /api/matches/{id} - Delete match</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _matchService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>GET /api/matches/user/{userId} - Get all matches for a user</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<MatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var matches = await _matchService.GetByUserAsync(userId);
        return Ok(matches);
    }

    /// <summary>GET /api/matches/season/{seasonId} - Get all matches in a season</summary>
    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(List<MatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySeason(int seasonId)
    {
        var matches = await _matchService.GetBySeasonAsync(seasonId);
        return Ok(matches);
    }
}

