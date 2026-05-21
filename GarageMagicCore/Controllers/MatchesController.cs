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

    /// <summary>GET /api/matches/sheriff-roles - Role metadata for UI (colors, labels, win conditions)</summary>
    [HttpGet("sheriff-roles")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SheriffRoleMetadataDto>), StatusCodes.Status200OK)]
    public IActionResult GetSheriffRoles()
    {
        var roles = new List<SheriffRoleMetadataDto>
        {
            new()
            {
                Role = "Sheriff",
                Label = "Sheriff",
                Color = "#FFFFFF",
                TextColor = "#000000",
                MaxCount = 1,
                GameModeOnly = null,
                WinCondition = "Survive the game. Deputy wins with you."
            },
            new()
            {
                Role = "Deputy",
                Label = "Deputy",
                Color = "#3B82F6",
                TextColor = "#FFFFFF",
                MaxCount = 1,
                GameModeOnly = null,
                WinCondition = "Sheriff's team wins. You win even if you die, as long as the Sheriff survives."
            },
            new()
            {
                Role = "Outlaw",
                Label = "Outlaw",
                Color = "#EF4444",
                TextColor = "#FFFFFF",
                MaxCount = 2,
                GameModeOnly = null,
                WinCondition = "Kill the Sheriff. Both Outlaws win if the Sheriff dies, even if one is already dead. Delivering the killing blow triggers the Matriarch swap."
            },
            new()
            {
                Role = "Renegade",
                Label = "Renegade",
                Color = "#111827",
                TextColor = "#FFFFFF",
                MaxCount = 1,
                GameModeOnly = "SixPlayerSheriff",
                WinCondition = "Be the last player standing. You cannot kill the Sheriff first — eliminate Outlaws and the Deputy before going for the Sheriff."
            },
            new()
            {
                Role = "Matriarch",
                Label = "Matriarch",
                Color = "#7C3AED",
                TextColor = "#FFFFFF",
                MaxCount = 1,
                GameModeOnly = null,
                WinCondition = "Dealt as a starting role. If you deliver the killing blow to the Sheriff, you become the new Sheriff (life resets), the old Sheriff drops to 1 HP and becomes an Outlaw, and the Deputy now fights for you. Win as the new Sheriff to claim victory."
            }
        };

        return Ok(roles);
    }
}

