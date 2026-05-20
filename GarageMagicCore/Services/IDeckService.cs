using GarageMagicCore.DTOs.User;
using GarageMagicCore.DTOs.Deck;

namespace GarageMagicCore.Services;

public interface IDeckService
{
    Task<DeckDto> CreateAsync(int userId, CreateDeckDto dto);
    Task<DeckDto?> GetByIdAsync(int id);
    Task<DeckDto?> UpdateAsync(int id, UpdateDeckDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<DeckDto>> GetByUserAsync(int userId);
    Task<DeckWithStatsDto?> GetWithStatsAsync(int id);
}

