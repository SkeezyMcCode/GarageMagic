using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Deck;
using GarageMagicCore.Models;
using System.Text.Json;

namespace GarageMagicCore.Services;

public class DeckService : IDeckService
{
    private readonly GarageMagicDbContext _context;

    public DeckService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<DeckDto> CreateAsync(int userId, CreateDeckDto dto)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            throw new InvalidOperationException($"User with ID {userId} not found.");

        var deck = new Deck
        {
            UserId = userId,
            DeckName = dto.DeckName,
            CommanderName = dto.CommanderName,
            ColorIdentity = dto.ColorIdentity,
            IsActive = true
        };

        _context.Decks.Add(deck);
        await _context.SaveChangesAsync();
        return MapToDto(deck);
    }

    public async Task<DeckDto?> GetByIdAsync(int id)
    {
        var deck = await _context.Decks.FindAsync(id);
        return deck == null ? null : MapToDto(deck);
    }

    public async Task<DeckDto?> UpdateAsync(int id, UpdateDeckDto dto)
    {
        var deck = await _context.Decks.FindAsync(id);
        if (deck == null) return null;

        if (dto.DeckName != null) deck.DeckName = dto.DeckName;
        if (dto.CommanderName != null) deck.CommanderName = dto.CommanderName;
        if (dto.ColorIdentity != null) deck.ColorIdentity = dto.ColorIdentity;
        if (dto.IsActive.HasValue) deck.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();
        return MapToDto(deck);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var deck = await _context.Decks.FindAsync(id);
        if (deck == null) return false;

        // Soft delete
        deck.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<DeckDto>> GetByUserAsync(int userId)
    {
        return await _context.Decks
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.DeckName)
            .Select(d => MapToDto(d))
            .ToListAsync();
    }

    public async Task<DeckWithStatsDto?> GetWithStatsAsync(int id)
    {
        var deck = await _context.Decks.FindAsync(id);
        if (deck == null) return null;

        // Tally wins from all UserStats that reference this deck
        var allStats = await _context.UserStats
            .Where(s => s.WinsPerDeckJson != null)
            .Select(s => s.WinsPerDeckJson!)
            .ToListAsync();

        int totalWins = 0;
        foreach (var json in allStats)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<int, int>>(json);
            if (dict != null && dict.TryGetValue(id, out var w))
                totalWins += w;
        }

        // Count matches where this deck was used
        int totalMatches = await _context.MatchParticipants.CountAsync(mp => mp.DeckId == id);
        int losses = totalMatches - totalWins;

        return new DeckWithStatsDto
        {
            Id = deck.Id,
            UserId = deck.UserId,
            DeckName = deck.DeckName,
            CommanderName = deck.CommanderName,
            ColorIdentity = deck.ColorIdentity,
            IsActive = deck.IsActive,
            CreatedAt = deck.CreatedAt,
            TotalMatches = totalMatches,
            Wins = totalWins,
            Losses = losses < 0 ? 0 : losses,
            WinRate = totalMatches > 0 ? Math.Round((decimal)totalWins / totalMatches * 100, 2) : 0
        };
    }

    private static DeckDto MapToDto(Deck deck) => new()
    {
        Id = deck.Id,
        UserId = deck.UserId,
        DeckName = deck.DeckName,
        CommanderName = deck.CommanderName,
        ColorIdentity = deck.ColorIdentity,
        IsActive = deck.IsActive,
        CreatedAt = deck.CreatedAt
    };
}

