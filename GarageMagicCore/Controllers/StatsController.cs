using Microsoft.AspNetCore.Mvc;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.DTOs.Stats;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    /// <summary>GET /api/stats/leaderboard?seasonId={id} - Get leaderboard (defaults to active season)</summary>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(List<UserStandingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int? seasonId = null)
    {
        var leaderboard = await _statsService.GetLeaderboardAsync(seasonId);
        return Ok(leaderboard);
    }

    /// <summary>GET /api/stats/user/{userId}/season/{seasonId} - Get stats for a user in a season</summary>
    [HttpGet("user/{userId:int}/season/{seasonId:int}")]
    [ProducesResponseType(typeof(UserStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserStats(int userId, int seasonId)
    {
        var stats = await _statsService.GetUserStatsAsync(userId, seasonId);
        return stats == null ? NotFound() : Ok(stats);
    }

    /// <summary>GET /api/stats/deck/{userId}/season/{seasonId} - Get deck performance for a user in a season</summary>
    [HttpGet("deck/{userId:int}/season/{seasonId:int}")]
    [ProducesResponseType(typeof(List<DeckStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeckStats(int userId, int seasonId)
    {
        var stats = await _statsService.GetDeckStatsAsync(userId, seasonId);
        return Ok(stats);
    }
}

