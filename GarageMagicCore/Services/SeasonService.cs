using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.Models;

namespace GarageMagicCore.Services;

public class SeasonService : ISeasonService
{
    private readonly GarageMagicDbContext _context;

    public SeasonService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<SeasonDto?> GetCurrentAsync()
    {
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
        return season == null ? null : MapToDto(season);
    }

    public async Task<SeasonDto?> GetByIdAsync(int id)
    {
        var season = await _context.Seasons.FindAsync(id);
        return season == null ? null : MapToDto(season);
    }

    public async Task<List<SeasonDto>> GetAllAsync()
    {
        return await _context.Seasons
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Quarter)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<SeasonDto> CreateAsync(CreateSeasonDto dto)
    {
        if (await _context.Seasons.AnyAsync(s => s.Year == dto.Year && s.Quarter == dto.Quarter))
            throw new InvalidOperationException($"Season {dto.Year} {dto.Quarter} already exists.");

        var (startDate, endDate) = GetQuarterDates(dto.Year, dto.Quarter);

        var season = new Season
        {
            Name = dto.Name,
            Year = dto.Year,
            Quarter = dto.Quarter,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = false
        };

        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();
        return MapToDto(season);
    }

    public async Task<SeasonDto> RolloverAsync()
    {
        var current = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season to roll over from.");

        // Deactivate current season
        current.IsActive = false;

        // Calculate next quarter
        var (nextYear, nextQuarter) = GetNextQuarter(current.Year, current.Quarter);

        if (await _context.Seasons.AnyAsync(s => s.Year == nextYear && s.Quarter == nextQuarter))
            throw new InvalidOperationException($"Season {nextYear} {nextQuarter} already exists.");

        var (startDate, endDate) = GetQuarterDates(nextYear, nextQuarter);

        var next = new Season
        {
            Name = $"{nextYear} {nextQuarter}",
            Year = nextYear,
            Quarter = nextQuarter,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };

        _context.Seasons.Add(next);
        await _context.SaveChangesAsync();
        return MapToDto(next);
    }

    public async Task<SeasonStandingsDto?> GetStandingsAsync(int seasonId)
    {
        var season = await _context.Seasons.FindAsync(seasonId);
        if (season == null) return null;

        var stats = await _context.UserStats
            .Include(s => s.User)
            .Where(s => s.SeasonId == seasonId)
            .OrderByDescending(s => s.TotalWins)
            .ThenBy(s => s.TotalLosses)
            .ToListAsync();

        var standings = stats.Select(s => new UserStandingDto
        {
            UserId = s.UserId,
            Username = s.User.Username,
            PrestigeLevel = s.User.CurrentPrestigeLevel,
            TotalWins = s.TotalWins,
            TotalLosses = s.TotalLosses,
            TotalMatches = s.TotalMatches,
            WinRate = s.TotalMatches > 0
                ? Math.Round((decimal)s.TotalWins / s.TotalMatches * 100, 2)
                : 0
        }).ToList();

        return new SeasonStandingsDto
        {
            Season = MapToDto(season),
            Standings = standings
        };
    }

    private static (DateTime start, DateTime end) GetQuarterDates(int year, Quarter quarter) =>
        quarter switch
        {
            Quarter.Q1 => (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                           new DateTime(year, 3, 31, 23, 59, 59, DateTimeKind.Utc)),
            Quarter.Q2 => (new DateTime(year, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                           new DateTime(year, 6, 30, 23, 59, 59, DateTimeKind.Utc)),
            Quarter.Q3 => (new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                           new DateTime(year, 9, 30, 23, 59, 59, DateTimeKind.Utc)),
            Quarter.Q4 => (new DateTime(year, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                           new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc)),
            _ => throw new ArgumentOutOfRangeException(nameof(quarter))
        };

    private static (int year, Quarter quarter) GetNextQuarter(int year, Quarter quarter) =>
        quarter == Quarter.Q4 ? (year + 1, Quarter.Q1) : (year, quarter + 1);

    private static SeasonDto MapToDto(Season s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Year = s.Year,
        Quarter = s.Quarter,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        IsActive = s.IsActive
    };
}

