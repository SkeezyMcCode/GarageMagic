using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Betrayal;
using GarageMagicCore.Models;

namespace GarageMagicCore.Services;

public class BetrayalService : IBetrayalService
{
    private readonly GarageMagicDbContext _context;

    public BetrayalService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<BetrayalDto> CreateAsync(CreateBetrayalDto dto)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == dto.BetrayerUserId))
            throw new InvalidOperationException($"Betrayer user ID {dto.BetrayerUserId} not found.");

        if (!await _context.Users.AnyAsync(u => u.Id == dto.VictimUserId))
            throw new InvalidOperationException($"Victim user ID {dto.VictimUserId} not found.");

        if (dto.BetrayerUserId == dto.VictimUserId)
            throw new InvalidOperationException("A user cannot betray themselves.");

        var betrayal = new Betrayal
        {
            BetrayerUserId = dto.BetrayerUserId,
            VictimUserId = dto.VictimUserId,
            Description = dto.Description,
            BetrayalDate = dto.BetrayalDate == default ? DateTime.UtcNow : dto.BetrayalDate
        };

        _context.Betrayals.Add(betrayal);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(betrayal).Reference(b => b.BetrayerUser).LoadAsync();
        await _context.Entry(betrayal).Reference(b => b.VictimUser).LoadAsync();

        return MapToDto(betrayal);
    }

    public async Task<List<BetrayalDto>> GetByUserAsync(int userId)
    {
        var betrayals = await _context.Betrayals
            .Include(b => b.BetrayerUser)
            .Include(b => b.VictimUser)
            .Where(b => b.BetrayerUserId == userId || b.VictimUserId == userId)
            .OrderByDescending(b => b.BetrayalDate)
            .ToListAsync();

        return betrayals.Select(MapToDto).ToList();
    }

    public async Task<List<BetrayalDto>> GetRecentAsync(int count = 10)
    {
        var betrayals = await _context.Betrayals
            .Include(b => b.BetrayerUser)
            .Include(b => b.VictimUser)
            .OrderByDescending(b => b.BetrayalDate)
            .Take(count)
            .ToListAsync();

        return betrayals.Select(MapToDto).ToList();
    }

    private static BetrayalDto MapToDto(Betrayal b) => new()
    {
        Id = b.Id,
        BetrayerUserId = b.BetrayerUserId,
        BetrayerUsername = b.BetrayerUser.Username,
        VictimUserId = b.VictimUserId,
        VictimUsername = b.VictimUser.Username,
        Description = b.Description,
        BetrayalDate = b.BetrayalDate,
        CreatedAt = b.CreatedAt
    };
}

