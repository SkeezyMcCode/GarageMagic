using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Auth;
using GarageMagicCore.Models;

namespace GarageMagicCore.Services;

public class AuthService : IAuthService
{
    private readonly GarageMagicDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(GarageMagicDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return null; // Invalid credentials

        if (!user.IsApproved)
            throw new UnauthorizedAccessException("pending");

        return new AuthResponseDto
        {
            Token = GenerateToken(user),
            User = new AuthUserDto { Id = user.Id, Username = user.Username, IsAdmin = user.IsAdmin }
        };
    }

    private string GenerateToken(User user)
    {
        var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "GarageMagic";
        var audience = _config["Jwt:Audience"] ?? "GarageMagicApp";
        var expiryDays = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 30;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

