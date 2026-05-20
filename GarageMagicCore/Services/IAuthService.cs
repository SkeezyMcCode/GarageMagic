using GarageMagicCore.DTOs.Auth;

namespace GarageMagicCore.Services;

public interface IAuthService
{
    /// <summary>Validates credentials and returns a JWT. Returns null if credentials invalid. Throws if account pending approval.</summary>
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}

