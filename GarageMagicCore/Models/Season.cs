namespace GarageMagicCore.Models;

public class Season
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Year { get; set; }
    public Quarter Quarter { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<UserStats> UserStats { get; set; } = new List<UserStats>();
    public ICollection<PrestigeLevel> PrestigeLevels { get; set; } = new List<PrestigeLevel>();
}

public enum Quarter
{
    Q1,  // January-March
    Q2,  // April-June
    Q3,  // July-September
    Q4   // October-December
}

