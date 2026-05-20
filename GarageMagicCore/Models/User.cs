namespace GarageMagicCore.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public int CurrentPrestigeLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsApproved { get; set; } = false;
    public bool IsAdmin { get; set; } = false;

    // Navigation properties
    public ICollection<Deck> Decks { get; set; } = new List<Deck>();
    public ICollection<MatchWinner> MatchesAsWinner { get; set; } = new List<MatchWinner>();
    public ICollection<UserStats> Stats { get; set; } = new List<UserStats>();
    public ICollection<PrestigeLevel> PrestigeLevels { get; set; } = new List<PrestigeLevel>();
    public ICollection<Betrayal> BetrayalsAsBetrayer { get; set; } = new List<Betrayal>();
    public ICollection<Betrayal> BetrayalsAsVictim { get; set; } = new List<Betrayal>();
}


