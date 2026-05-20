using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.DTOs.Stats;
using System.Text.Json;

namespace GarageMagicCore.Services;

public class StatsService : IStatsService
{
    private readonly GarageMagicDbContext _context;

    public StatsService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserStandingDto>> GetLeaderboardAsync(int? seasonId = null)
    {
        int targetSeasonId;
        if (seasonId.HasValue)
        {
            targetSeasonId = seasonId.Value;
        }
        else
        {
            var active = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
            if (active == null) return new List<UserStandingDto>();
            targetSeasonId = active.Id;
        }

        var stats = await _context.UserStats
            .Include(s => s.User)
            .Where(s => s.SeasonId == targetSeasonId)
            .OrderByDescending(s => s.TotalWins)
            .ThenBy(s => s.TotalLosses)
            .ToListAsync();

        return stats.Select(s => new UserStandingDto
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
    }

    public async Task<UserStatsDto?> GetUserStatsAsync(int userId, int seasonId)
    {
        var stats = await _context.UserStats
            .Include(s => s.User)
            .Include(s => s.Season)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeasonId == seasonId);

        if (stats == null) return null;

        return new UserStatsDto
        {
            UserId = stats.UserId,
            Username = stats.User.Username,
            SeasonId = stats.SeasonId,
            SeasonName = stats.Season.Name,
            TotalWins = stats.TotalWins,
            TotalLosses = stats.TotalLosses,
            TotalMatches = stats.TotalMatches,
            WinRate = stats.TotalMatches > 0
                ? Math.Round((decimal)stats.TotalWins / stats.TotalMatches * 100, 2)
                : 0,
            Wins1v1v1 = stats.Wins1v1v1,
            Wins1v1v1v1 = stats.Wins1v1v1v1,
            WinsSheriff = stats.WinsSheriff,
            SheriffGamesPlayed = stats.SheriffGamesPlayed,
            SheriffGamesWon = stats.SheriffGamesWon,
            DeputyGamesPlayed = stats.DeputyGamesPlayed,
            DeputyGamesWon = stats.DeputyGamesWon,
            RedGamesPlayed = stats.RedGamesPlayed,
            RedGamesWon = stats.RedGamesWon,
            PrestigeLevel = stats.User.CurrentPrestigeLevel
        };
    }

    public async Task<List<DeckStatsDto>> GetDeckStatsAsync(int userId, int seasonId)
    {
        var stats = await _context.UserStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeasonId == seasonId);

        if (stats?.WinsPerDeckJson == null) return new List<DeckStatsDto>();

        var deckWins = JsonSerializer.Deserialize<Dictionary<int, int>>(stats.WinsPerDeckJson)
                       ?? new Dictionary<int, int>();

        var deckIds = deckWins.Keys.ToList();
        var decks = await _context.Decks
            .Where(d => deckIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        return deckWins.Select(kv =>
        {
            decks.TryGetValue(kv.Key, out var deck);
            return new DeckStatsDto
            {
                DeckId = kv.Key,
                DeckName = deck?.DeckName ?? "Unknown Deck",
                CommanderName = deck?.CommanderName ?? "Unknown Commander",
                TotalWins = kv.Value
            };
        })
        .OrderByDescending(d => d.TotalWins)
        .ToList();
    }
}

