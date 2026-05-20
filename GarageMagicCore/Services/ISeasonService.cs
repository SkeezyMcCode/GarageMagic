using GarageMagicCore.DTOs.Season;

namespace GarageMagicCore.Services;

public interface ISeasonService
{
    Task<SeasonDto?> GetCurrentAsync();
    Task<SeasonDto?> GetByIdAsync(int id);
    Task<List<SeasonDto>> GetAllAsync();
    Task<SeasonDto> CreateAsync(CreateSeasonDto dto);
    Task<SeasonDto> RolloverAsync();
    Task<SeasonStandingsDto?> GetStandingsAsync(int seasonId);
}

