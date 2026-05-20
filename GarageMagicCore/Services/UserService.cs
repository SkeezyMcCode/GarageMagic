using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.User;
using GarageMagicCore.Models;

namespace GarageMagicCore.Services;

public class UserService : IUserService
{
    private readonly GarageMagicDbContext _context;

    public UserService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> RegisterAsync(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsApproved = false,
            IsAdmin = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");
            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserWithStatsDto?> GetWithStatsAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Decks)
            .Include(u => u.Stats)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return null;

        var totalWins = user.Stats.Sum(s => s.TotalWins);
        var totalLosses = user.Stats.Sum(s => s.TotalLosses);
        var totalMatches = totalWins + totalLosses;

        return new UserWithStatsDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CurrentPrestigeLevel = user.CurrentPrestigeLevel,
            CreatedAt = user.CreatedAt,
            TotalDecks = user.Decks.Count,
            TotalWins = totalWins,
            TotalLosses = totalLosses,
            WinRate = totalMatches > 0 ? Math.Round((decimal)totalWins / totalMatches * 100, 2) : 0
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PendingUserDto>> GetPendingAsync()
    {
        return await _context.Users
            .Where(u => !u.IsApproved)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new PendingUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserDto?> ApproveAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;
        user.IsApproved = true;
        await _context.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<bool> RejectAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        CurrentPrestigeLevel = user.CurrentPrestigeLevel,
        CreatedAt = user.CreatedAt,
        IsApproved = user.IsApproved,
        IsAdmin = user.IsAdmin
    };
}

