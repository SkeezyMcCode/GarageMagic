using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GarageMagicCore.DTOs.Betrayal;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/betrayals")]
[Authorize]
public class BetrayalsController : ControllerBase
{
    private readonly IBetrayalService _betrayalService;

    public BetrayalsController(IBetrayalService betrayalService)
    {
        _betrayalService = betrayalService;
    }

    /// <summary>POST /api/betrayals - Record a betrayal</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BetrayalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBetrayalDto dto)
    {
        try
        {
            var betrayal = await _betrayalService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByUser), new { userId = dto.BetrayerUserId }, betrayal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/betrayals/user/{userId} - Get all betrayals involving a user</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<BetrayalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var betrayals = await _betrayalService.GetByUserAsync(userId);
        return Ok(betrayals);
    }

    /// <summary>GET /api/betrayals/recent?count={n} - Get most recent betrayals</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<BetrayalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        var betrayals = await _betrayalService.GetRecentAsync(count);
        return Ok(betrayals);
    }

    /// <summary>DELETE /api/betrayals/{id} - Delete a betrayal (Admin only)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _betrayalService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>PATCH /api/betrayals/{id} - Update a betrayal's description and/or date (Admin only)</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BetrayalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBetrayalDto dto)
    {
        if (dto.Description != null && string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { error = "Description cannot be empty." });

        var result = await _betrayalService.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }
}

