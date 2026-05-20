using GarageMagicCore.DTOs.Season;
using GarageMagicCore.DTOs.Stats;

namespace GarageMagicCore.Services;

public interface IStatsService
{
    Task<List<UserStandingDto>> GetLeaderboardAsync(int? seasonId = null);
    Task<UserStatsDto?> GetUserStatsAsync(int userId, int seasonId);
    Task<List<DeckStatsDto>> GetDeckStatsAsync(int userId, int seasonId);
}

