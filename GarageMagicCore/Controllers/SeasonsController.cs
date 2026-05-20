using Microsoft.AspNetCore.Mvc;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/seasons")]
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

    /// <summary>GET /api/seasons/{id}/standings - Get season standings</summary>
    [HttpGet("{id:int}/standings")]
    [ProducesResponseType(typeof(SeasonStandingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandings(int id)
    {
        var standings = await _seasonService.GetStandingsAsync(id);
        return standings == null ? NotFound() : Ok(standings);
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

