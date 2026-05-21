using GarageMagicCore.Models;

namespace GarageMagicCore.DTOs.Match;

/// <summary>
/// DTO for creating a new match
/// </summary>
public class CreateMatchDto
{
    public int? DeckId { get; set; }
    public required Models.MatchType MatchType { get; set; }
    public DateTime MatchDate { get; set; }
    public required List<MatchParticipantDto> Participants { get; set; }
    public required List<int> WinnerUserIds { get; set; }
    /// <summary>Outlaw who delivered the killing blow to the Sheriff and became the new Sheriff</summary>
    public int? MatriarchUserId { get; set; }
}

/// <summary>
/// DTO for match participant
/// </summary>
public class MatchParticipantDto
{
    public int UserId { get; set; }
    public int? DeckId { get; set; }
    public HiddenRole? HiddenRole { get; set; }
    /// <summary>Role at game end — only differs from HiddenRole for Matriarch swap players</summary>
    public HiddenRole? FinalRole { get; set; }
}

/// <summary>
/// DTO for match response
/// </summary>
public class MatchDto
{
    public int Id { get; set; }
    public int? DeckId { get; set; }
    public Models.MatchType MatchType { get; set; }
    public DateTime MatchDate { get; set; }
    public int? SheriffUserId { get; set; }
    public int? MatriarchUserId { get; set; }
    public List<MatchWinnerDto> Winners { get; set; } = new();
    public List<MatchParticipantDetailDto> Participants { get; set; } = new();
}

/// <summary>
/// DTO for match winner details
/// </summary>
public class MatchWinnerDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
}

/// <summary>
/// DTO for match participant details
/// </summary>
public class MatchParticipantDetailDto
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public int? DeckId { get; set; }
    public string? DeckName { get; set; }
    public HiddenRole? HiddenRole { get; set; }
    public HiddenRole? FinalRole { get; set; }
}

/// <summary>Returned by GET /api/matches/sheriff-roles — used by frontend to render role buttons/colors</summary>
public class SheriffRoleMetadataDto
{
    public required string Role { get; set; }
    public required string Label { get; set; }
    public required string Color { get; set; }
    public required string TextColor { get; set; }
    public int MaxCount { get; set; }
    public string? GameModeOnly { get; set; }
    public required string WinCondition { get; set; }
}
