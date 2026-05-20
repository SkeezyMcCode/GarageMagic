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

