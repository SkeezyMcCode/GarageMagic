using GarageMagicCore.Models;

namespace GarageMagicCore.DTOs.Season;

/// <summary>
/// DTO for creating a new season
/// </summary>
public class CreateSeasonDto
{
    public required string Name { get; set; }
    public int Year { get; set; }
    public Quarter Quarter { get; set; }
}

/// <summary>
/// DTO for season response
/// </summary>
public class SeasonDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Year { get; set; }
    public Quarter Quarter { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for season standings
/// </summary>
public class SeasonStandingsDto
{
    public SeasonDto Season { get; set; } = null!;
    public List<UserStandingDto> Standings { get; set; } = new();
}

/// <summary>
/// DTO for user standing in a season
/// </summary>
public class UserStandingDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public int PrestigeLevel { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalMatches { get; set; }
    public decimal WinRate { get; set; }
}

