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

        bool isSheriff = dto.MatchType is MatchType.FivePlayerSheriff or MatchType.SixPlayerSheriff;

        // For Sheriff games, resolve winners server-side based on role rules
        var resolvedWinnerIds = isSheriff
            ? ResolveSheriffWinners(dto)
            : dto.WinnerUserIds;

        int? sheriffUserId = dto.Participants
            .FirstOrDefault(p => p.HiddenRole == HiddenRole.Sheriff)?.UserId;

        var match = new Match
        {
            DeckId = dto.DeckId,
            MatchType = dto.MatchType,
            MatchDate = dto.MatchDate == default ? DateTime.UtcNow : dto.MatchDate,
            SheriffUserId = sheriffUserId,
            MatriarchUserId = dto.MatriarchUserId
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        foreach (var p in dto.Participants)
        {
            _context.MatchParticipants.Add(new MatchParticipant
            {
                MatchId = match.Id,
                UserId = p.UserId,
                DeckId = p.DeckId,
                HiddenRole = p.HiddenRole,
                FinalRole = p.FinalRole ?? p.HiddenRole
            });
        }

        foreach (var winnerId in resolvedWinnerIds)
        {
            _context.MatchWinners.Add(new MatchWinner
            {
                MatchId = match.Id,
                UserId = winnerId
            });
        }

        await _context.SaveChangesAsync();

        var settingValue = await _context.AppSettings
            .Where(s => s.SettingKey == SettingKeys.WinsPerPrestigeLevel)
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync();
        int winsPerPrestige = int.TryParse(settingValue, out var w) ? w : 5;

        foreach (var p in dto.Participants)
        {
            var stats = await _context.UserStats
                .FirstOrDefaultAsync(s => s.UserId == p.UserId && s.SeasonId == season.Id);

            if (stats == null)
            {
                stats = new UserStats { UserId = p.UserId, SeasonId = season.Id };
                _context.UserStats.Add(stats);
            }

            bool isWinner = resolvedWinnerIds.Contains(p.UserId);
            stats.TotalMatches++;

            if (isWinner)
            {
                stats.TotalWins++;
                stats.Wins1v1v1 += dto.MatchType == MatchType.OneVsOneVsOne ? 1 : 0;
                stats.Wins1v1v1v1 += dto.MatchType == MatchType.OneVsOneVsOneVsOne ? 1 : 0;
                stats.WinsSheriff += isSheriff ? 1 : 0;

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

            if (isSheriff && p.HiddenRole.HasValue)
            {
                // Use FinalRole for stat bucketing so Matriarch shows as Sheriff at end
                var roleForStats = p.FinalRole ?? p.HiddenRole.Value;

                switch (roleForStats)
                {
                    case HiddenRole.Sheriff:
                        stats.SheriffGamesPlayed++;
                        if (isWinner) stats.SheriffGamesWon++;
                        break;
                    case HiddenRole.Deputy:
                        stats.DeputyGamesPlayed++;
                        if (isWinner) stats.DeputyGamesWon++;
                        break;
                    case HiddenRole.Outlaw:
                        stats.OutlawGamesPlayed++;
                        if (isWinner) stats.OutlawGamesWon++;
                        break;
                    case HiddenRole.Renegade:
                        stats.RenegadeGamesPlayed++;
                        if (isWinner) stats.RenegadeGamesWon++;
                        break;
                }

                // Matriarch: started as Matriarch role
                if (p.HiddenRole == HiddenRole.Matriarch)
                {
                    stats.MatriarchGamesPlayed++;
                    // Triggered = they actually killed the Sheriff and swapped
                    if (dto.MatriarchUserId == p.UserId)
                    {
                        stats.MatriarchTriggered++;
                        if (isWinner) stats.MatriarchWins++;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        foreach (var winnerId in resolvedWinnerIds)
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

    /// <summary>
    /// Resolves winners for Sheriff games based on role rules:
    /// - Sheriff team wins → Deputy auto-added as winner even if dead
    /// - Sheriff dies → Outlaws + Matriarch (if they triggered swap) win; Deputy removed
    /// - If Matriarch triggered swap: game continued, Matriarch became new Sheriff — use them as final Sheriff
    /// - Renegade: frontend passes them in WinnerUserIds if they won
    /// </summary>
    private static List<int> ResolveSheriffWinners(CreateMatchDto dto)
    {
        var sheriffParticipant = dto.Participants.First(p => p.HiddenRole == HiddenRole.Sheriff);

        // If Matriarch triggered the swap, they are the final Sheriff for win resolution
        int finalSheriffId = dto.MatriarchUserId ?? sheriffParticipant.UserId;
        bool finalSheriffWon = dto.WinnerUserIds.Contains(finalSheriffId);

        var winners = new HashSet<int>(dto.WinnerUserIds);

        if (finalSheriffWon)
        {
            // Sheriff team wins — Deputy also wins regardless of whether they're dead
            var deputyId = dto.Participants.FirstOrDefault(p => p.HiddenRole == HiddenRole.Deputy)?.UserId;
            if (deputyId.HasValue)
                winners.Add(deputyId.Value);
        }
        else
        {
            // Sheriff died — Deputy can't win; Outlaws win
            var deputyId = dto.Participants.FirstOrDefault(p => p.HiddenRole == HiddenRole.Deputy)?.UserId;
            if (deputyId.HasValue)
                winners.Remove(deputyId.Value);

            foreach (var outlaw in dto.Participants.Where(p => p.HiddenRole == HiddenRole.Outlaw))
                winners.Add(outlaw.UserId);

            // Matriarch only wins if THEY specifically triggered the swap (and then won as new Sheriff)
            // If Sheriff died to an Outlaw instead, Matriarch does not automatically win
        }

        return winners.ToList();
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
        MatriarchUserId = match.MatriarchUserId,
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
            HiddenRole = p.HiddenRole,
            FinalRole = p.FinalRole
        }).ToList()
    };
}


