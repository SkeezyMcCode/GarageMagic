using GarageMagicCore.DTOs.Betrayal;

namespace GarageMagicCore.Services;

public interface IBetrayalService
{
    Task<BetrayalDto> CreateAsync(CreateBetrayalDto dto);
    Task<List<BetrayalDto>> GetByUserAsync(int userId);
    Task<List<BetrayalDto>> GetRecentAsync(int count = 10);
    Task<bool> DeleteAsync(int id);
    Task<BetrayalDto?> UpdateAsync(int id, UpdateBetrayalDto dto);
}

