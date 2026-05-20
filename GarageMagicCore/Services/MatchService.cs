using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Match;
using GarageMagicCore.Models;
using System.Text.Json;
using MatchType = GarageMagicCore.Models.MatchType;

namespace GarageMagicCore.Services;

public class MatchService : IMatchService
{
    private readonly GarageMagicDbContext _context;

    public MatchService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<MatchDto> CreateAsync(CreateMatchDto dto)
    {
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive)
            ?? throw new InvalidOperationException("No active season found. Please create a season first.");

        // Determine sheriff from participants
        int? sheriffUserId = dto.Participants
            .FirstOrDefault(p => p.HiddenRole == HiddenRole.Sheriff)?.UserId;

        var match = new Match
        {
            DeckId = dto.DeckId,
            MatchType = dto.MatchType,
            MatchDate = dto.MatchDate == default ? DateTime.UtcNow : dto.MatchDate,
            SheriffUserId = sheriffUserId
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        // Create participants
        foreach (var p in dto.Participants)
        {
            _context.MatchParticipants.Add(new MatchParticipant
            {
                MatchId = match.Id,
                UserId = p.UserId,
                DeckId = p.DeckId,
                HiddenRole = p.HiddenRole
            });
        }

        // Create winners
        foreach (var winnerId in dto.WinnerUserIds)
        {
            _context.MatchWinners.Add(new MatchWinner
            {
                MatchId = match.Id,
                UserId = winnerId
            });
        }

        await _context.SaveChangesAsync();

        // Get WinsPerPrestigeLevel setting
        var settingValue = await _context.AppSettings
            .Where(s => s.SettingKey == SettingKeys.WinsPerPrestigeLevel)
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync();
        int winsPerPrestige = int.TryParse(settingValue, out var w) ? w : 5;

        bool isSheriff = dto.MatchType is MatchType.FivePlayerSheriff or MatchType.SixPlayerSheriff;

        // Update UserStats for each participant
        foreach (var p in dto.Participants)
        {
            var stats = await _context.UserStats
                .FirstOrDefaultAsync(s => s.UserId == p.UserId && s.SeasonId == season.Id);

            if (stats == null)
            {
                stats = new UserStats { UserId = p.UserId, SeasonId = season.Id };
                _context.UserStats.Add(stats);
            }

            bool isWinner = dto.WinnerUserIds.Contains(p.UserId);
            stats.TotalMatches++;

            if (isWinner)
            {
                stats.TotalWins++;
                stats.Wins1v1v1 += dto.MatchType == MatchType.OneVsOneVsOne ? 1 : 0;
                stats.Wins1v1v1v1 += dto.MatchType == MatchType.OneVsOneVsOneVsOne ? 1 : 0;
                stats.WinsSheriff += isSheriff ? 1 : 0;

                // Update deck win JSON
                if (p.DeckId.HasValue)
                {
                    var deckWins = string.IsNullOrEmpty(stats.WinsPerDeckJson)
                        ? new Dictionary<int, int>()
                        : JsonSerializer.Deserialize<Dictionary<int, int>>(stats.WinsPerDeckJson) ?? new();

                    deckWins[p.DeckId.Value] = deckWins.GetValueOrDefault(p.DeckId.Value) + 1;
                    stats.WinsPerDeckJson = JsonSerializer.Serialize(deckWins);
                }
            }
            else
            {
                stats.TotalLosses++;
            }

            // Sheriff role stats
            if (isSheriff && p.HiddenRole.HasValue)
            {
                switch (p.HiddenRole.Value)
                {
                    case HiddenRole.Sheriff:
                        stats.SheriffGamesPlayed++;
                        if (isWinner) stats.SheriffGamesWon++;
                        break;
                    case HiddenRole.Deputy:
                        stats.DeputyGamesPlayed++;
                        if (isWinner) stats.DeputyGamesWon++;
                        break;
                    case HiddenRole.Red:
                        stats.RedGamesPlayed++;
                        if (isWinner) stats.RedGamesWon++;
                        break;
                }
            }
        }

        await _context.SaveChangesAsync();

        // Update prestige for winners
        foreach (var winnerId in dto.WinnerUserIds)
        {
            var stats = await _context.UserStats
                .FirstAsync(s => s.UserId == winnerId && s.SeasonId == season.Id);
            var user = await _context.Users.FindAsync(winnerId);
            if (user == null) continue;

            int newLevel = stats.TotalWins / winsPerPrestige;
            if (newLevel > user.CurrentPrestigeLevel)
            {
                user.CurrentPrestigeLevel = newLevel;
                _context.PrestigeLevels.Add(new PrestigeLevel
                {
                    UserId = winnerId,
                    SeasonId = season.Id,
                    Level = newLevel,
                    AchievedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(match.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created match.");
    }

    public async Task<MatchDto?> GetByIdAsync(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Winners).ThenInclude(w => w.User)
            .Include(m => m.Participants).ThenInclude(p => p.User)
            .Include(m => m.Participants).ThenInclude(p => p.Deck)
            .FirstOrDefaultAsync(m => m.Id == id);

        return match == null ? null : MapToDto(match);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match == null) return false;

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MatchDto>> GetByUserAsync(int userId)
    {
        var matches = await _context.Matches
            .Include(m => m.Winners).ThenInclude(w => w.User)
            .Include(m => m.Participants).ThenInclude(p => p.User)
            .Include(m => m.Participants).ThenInclude(p => p.Deck)
            .Where(m => m.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync();

        return matches.Select(MapToDto).ToList();
    }

    public async Task<List<MatchDto>> GetBySeasonAsync(int seasonId)
    {
        var season = await _context.Seasons.FindAsync(seasonId);
        if (season == null) return new List<MatchDto>();

        var matches = await _context.Matches
            .Include(m => m.Winners).ThenInclude(w => w.User)
            .Include(m => m.Participants).ThenInclude(p => p.User)
            .Include(m => m.Participants).ThenInclude(p => p.Deck)
            .Where(m => m.MatchDate >= season.StartDate && m.MatchDate <= season.EndDate)
            .OrderByDescending(m => m.MatchDate)
            .ToListAsync();

        return matches.Select(MapToDto).ToList();
    }

    private static MatchDto MapToDto(Match match) => new()
    {
        Id = match.Id,
        DeckId = match.DeckId,
        MatchType = match.MatchType,
        MatchDate = match.MatchDate,
        SheriffUserId = match.SheriffUserId,
        Winners = match.Winners.Select(w => new MatchWinnerDto
        {
            UserId = w.UserId,
            Username = w.User.Username
        }).ToList(),
        Participants = match.Participants.Select(p => new MatchParticipantDetailDto
        {
            UserId = p.UserId,
            Username = p.User.Username,
            DeckId = p.DeckId,
            DeckName = p.Deck?.DeckName,
            HiddenRole = p.HiddenRole
        }).ToList()
    };
}


