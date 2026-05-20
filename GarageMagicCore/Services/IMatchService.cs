using GarageMagicCore.DTOs.Match;

namespace GarageMagicCore.Services;

public interface IMatchService
{
    Task<MatchDto> CreateAsync(CreateMatchDto dto);
    Task<MatchDto?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<List<MatchDto>> GetByUserAsync(int userId);
    Task<List<MatchDto>> GetBySeasonAsync(int seasonId);
}

