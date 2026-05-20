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
}

