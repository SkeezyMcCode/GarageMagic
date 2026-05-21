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

    /// <summary>Updates season name and date range. Returns null if not found.</summary>
    Task<SeasonDto?> UpdateAsync(int seasonId, UpdateSeasonDto dto);

    /// <summary>
    /// Upserts a user's win/loss record for a season and recomputes derived fields.
    /// Returns null if season or user is not found.
    /// </summary>
    Task<SeasonRecordDto?> UpsertRecordAsync(int seasonId, UpsertSeasonRecordDto dto);
}
