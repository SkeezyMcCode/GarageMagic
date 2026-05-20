namespace GarageMagicCore.Models;

public class Betrayal
{
    public int Id { get; set; }
    public int BetrayerUserId { get; set; }
    public int VictimUserId { get; set; }
    public required string Description { get; set; }
    public DateTime BetrayalDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User BetrayerUser { get; set; } = null!;
    public User VictimUser { get; set; } = null!;
}

