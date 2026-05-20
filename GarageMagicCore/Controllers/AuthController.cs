using Microsoft.AspNetCore.Mvc;
using GarageMagicCore.DTOs.Auth;
using GarageMagicCore.Services;

namespace GarageMagicCore.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>POST /api/auth/login - Login and receive a JWT</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(new { error = "Invalid username or password." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "pending")
        {
            return StatusCode(403, new { error = "Your account is pending admin approval." });
        }
    }
}

