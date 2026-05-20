namespace GarageMagicCore.DTOs.Betrayal;

/// <summary>
/// DTO for creating a new betrayal
/// </summary>
public class CreateBetrayalDto
{
    public int BetrayerUserId { get; set; }
    public int VictimUserId { get; set; }
    public required string Description { get; set; }
    public DateTime BetrayalDate { get; set; }
}

/// <summary>
/// DTO for betrayal response
/// </summary>
public class BetrayalDto
{
    public int Id { get; set; }
    public int BetrayerUserId { get; set; }
    public required string BetrayerUsername { get; set; }
    public int VictimUserId { get; set; }
    public required string VictimUsername { get; set; }
    public required string Description { get; set; }
    public DateTime BetrayalDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

