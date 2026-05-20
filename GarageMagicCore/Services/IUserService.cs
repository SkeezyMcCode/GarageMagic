using GarageMagicCore.DTOs.User;

namespace GarageMagicCore.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(CreateUserDto dto);
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto);
    Task<UserWithStatsDto?> GetWithStatsAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<List<PendingUserDto>> GetPendingAsync();
    Task<UserDto?> ApproveAsync(int id);
    Task<bool> RejectAsync(int id);
    Task<UserDto> CreateGuestAsync(CreateGuestDto dto);
    Task<List<UserDto>> GetGuestsAsync();
}

