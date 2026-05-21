using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.Deck;
using GarageMagicCore.Models;
using System.Text.Json;

namespace GarageMagicCore.Services;

public class DeckService : IDeckService
{
    private readonly GarageMagicDbContext _context;
    private readonly IScryfallService _scryfall;

    public DeckService(GarageMagicDbContext context, IScryfallService scryfall)
    {
        _context  = context;
        _scryfall = scryfall;
    }

    public async Task<DeckDto> CreateAsync(int userId, CreateDeckDto dto)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId))
            throw new InvalidOperationException($"User with ID {userId} not found.");

        // Auto-fetch commander image from Scryfall
        var cardInfo = await _scryfall.LookupCommanderAsync(dto.CommanderName);

        var deck = new Deck
        {
            UserId           = userId,
            DeckName         = dto.DeckName,
            CommanderName    = cardInfo?.Name ?? dto.CommanderName,
            ColorIdentity    = dto.ColorIdentity ?? (cardInfo?.ColorIdentity.Count > 0
                                   ? string.Join("", cardInfo.ColorIdentity)
                                   : null),
            CommanderImageUri = cardInfo?.ImageUri,
            ScryfallId        = cardInfo?.ScryfallId,
            IsActive          = true
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

        // If commander name changed, re-fetch image from Scryfall
        if (dto.CommanderName != null && dto.CommanderName != deck.CommanderName)
        {
            var cardInfo = await _scryfall.LookupCommanderAsync(dto.CommanderName);
            deck.CommanderName     = cardInfo?.Name ?? dto.CommanderName;
            deck.CommanderImageUri = cardInfo?.ImageUri;
            deck.ScryfallId        = cardInfo?.ScryfallId;

            // Auto-fill colour identity if caller didn't supply one
            if (dto.ColorIdentity == null && cardInfo?.ColorIdentity.Count > 0)
                deck.ColorIdentity = string.Join("", cardInfo.ColorIdentity);
        }

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
        Id                = deck.Id,
        UserId            = deck.UserId,
        DeckName          = deck.DeckName,
        CommanderName     = deck.CommanderName,
        ColorIdentity     = deck.ColorIdentity,
        IsActive          = deck.IsActive,
        CreatedAt         = deck.CreatedAt,
        CommanderImageUri = deck.CommanderImageUri,
        ScryfallId        = deck.ScryfallId
    };
}
