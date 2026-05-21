namespace GarageMagicCore.DTOs.User;

/// <summary>
/// DTO for creating a new user
/// </summary>
public class CreateUserDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// DTO for updating user information
/// </summary>
public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// DTO for user response
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public int CurrentPrestigeLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsGuest { get; set; }
}

/// <summary>
/// DTO for creating a guest player (admin only)
/// </summary>
public class CreateGuestDto
{
    public required string DisplayName { get; set; }
}

/// <summary>
/// DTO for a user awaiting admin approval
/// </summary>
public class PendingUserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for user with full stats
/// </summary>
public class UserWithStatsDto : UserDto
{
    public int TotalDecks { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public decimal WinRate { get; set; }
}

/// <summary>
/// Request body for POST /users/{pendingUserId}/approve-and-link.
/// Approves a pending user and migrates all history from the specified guest account.
/// </summary>
public class ApproveAndLinkDto
{
    public int GuestUserId { get; set; }
}

/// <summary>
/// Request body for POST /users/{id}/set-admin.</summary>
public class SetAdminDto
{
    public bool IsAdmin { get; set; }
}
