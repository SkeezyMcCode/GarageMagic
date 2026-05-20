namespace GarageMagicCore.Models;

public class Match
{
    public int Id { get; set; }
    public int? DeckId { get; set; }
    public MatchType MatchType { get; set; }
    public DateTime MatchDate { get; set; }
    public int? SheriffUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Deck? Deck { get; set; }
    public User? SheriffUser { get; set; }
    public ICollection<MatchWinner> Winners { get; set; } = new List<MatchWinner>();
    public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
}

public enum MatchType
{
    OneVsOneVsOne,      // 1v1v1 (3 players)
    OneVsOneVsOneVsOne, // 1v1v1v1 (4 players)
    FivePlayerSheriff,  // 5-player Sheriff
    SixPlayerSheriff    // 6-player Sheriff
}

