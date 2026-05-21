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

    /// <summary>
    /// Approves a pending user and migrates all history from a guest account in a single transaction.
    /// Throws <see cref="ArgumentException"/> if pendingUserId == guestUserId.
    /// Throws <see cref="KeyNotFoundException"/> if either user is not found.
    /// Throws <see cref="InvalidOperationException"/> if the pending user is already approved,
    /// or the guest user is not a guest.
    /// </summary>
    Task<UserDto> ApproveAndLinkAsync(int pendingUserId, int guestUserId);
}
