using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/seasons")]
[Authorize]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonsController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    /// <summary>GET /api/seasons - Get all seasons</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SeasonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var seasons = await _seasonService.GetAllAsync();
        return Ok(seasons);
    }

    /// <summary>GET /api/seasons/current - Get active season</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent()
    {
        var season = await _seasonService.GetCurrentAsync();
        return season == null ? NotFound(new { error = "No active season." }) : Ok(season);
    }

    /// <summary>GET /api/seasons/{id} - Get season by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var season = await _seasonService.GetByIdAsync(id);
        return season == null ? NotFound() : Ok(season);
    }

    /// <summary>POST /api/seasons - Create a new season</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSeasonDto dto)
    {
        try
        {
            var season = await _seasonService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = season.Id }, season);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PATCH /api/seasons/{id} — Admin-only. Update season name and date range.
    /// </summary>
    /// <param name="id">ID of the season to update.</param>
    /// <param name="dto">New name, startDate (ISO 8601), and endDate (ISO 8601).</param>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSeasonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "name is required." });

        if (dto.StartDate > dto.EndDate)
            return BadRequest(new { error = "startDate must be on or before endDate." });

        var season = await _seasonService.UpdateAsync(id, dto);
        return season == null ? NotFound(new { error = "Season not found." }) : Ok(season);
    }

    /// <summary>GET /api/seasons/{id}/standings - Get season standings</summary>
    [HttpGet("{id:int}/standings")]
    [ProducesResponseType(typeof(SeasonStandingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandings(int id)
    {
        var standings = await _seasonService.GetStandingsAsync(id);
        return standings == null ? NotFound() : Ok(standings);
    }

    /// <summary>
    /// PUT /api/seasons/{seasonId}/records/{userId} — Admin-only. Upsert a user's win/loss
    /// record for the given season and recompute totalMatches and winRate.
    /// </summary>
    /// <param name="seasonId">Target season.</param>
    /// <param name="userId">Target user.</param>
    /// <param name="dto">totalWins and totalLosses (both non-negative integers).</param>
    [HttpPut("{seasonId:int}/records/{userId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SeasonRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertRecord(int seasonId, int userId, [FromBody] UpsertSeasonRecordDto dto)
    {
        if (dto.TotalWins < 0 || dto.TotalLosses < 0)
            return BadRequest(new { error = "totalWins and totalLosses must be non-negative integers." });

        // Route param is authoritative; reject mismatched body value if provided
        if (dto.UserId != 0 && dto.UserId != userId)
            return BadRequest(new { error = "userId in body does not match the route parameter." });

        dto.UserId = userId;

        var record = await _seasonService.UpsertRecordAsync(seasonId, dto);
        return record == null ? NotFound(new { error = "Season or user not found." }) : Ok(record);
    }

    /// <summary>POST /api/seasons/rollover - Roll over to next season</summary>
    [HttpPost("rollover")]
    [ProducesResponseType(typeof(SeasonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Rollover()
    {
        try
        {
            var next = await _seasonService.RolloverAsync();
            return Ok(next);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

