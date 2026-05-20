namespace GarageMagicCore.DTOs.Auth;

public class LoginDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class AuthUserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public bool IsAdmin { get; set; }
}

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required AuthUserDto User { get; set; }
}

