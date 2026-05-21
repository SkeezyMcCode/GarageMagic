using Microsoft.EntityFrameworkCore;
readonly using Microsoft.EntityFrameworkCore.Diagnostics;
using GarageMagicCore.Data;
using GarageMagicCore.Models;

namespace GarageMagicCore.Tests.Helpers;

public static class TestDbHelper
{
    /// <summary>
    /// Creates a fresh in-memory DbContext with a unique database name per call,
    /// pre-seeded with AppSettings and an active season.
    /// The InMemory provider silently ignores transactions (no rollback semantics);
    /// TransactionIgnoredWarning is suppressed so service code that uses
    /// BeginTransactionAsync still compiles and runs without errors in tests.
    /// </summary>
    public static GarageMagicDbContext CreateContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<GarageMagicDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new GarageMagicDbContext(options);
        SeedDefaults(context);
        return context;
    }

    private static void SeedDefaults(GarageMagicDbContext context)
    {
        context.AppSettings.Add(new AppSettings
        {
            SettingKey = SettingKeys.WinsPerPrestigeLevel,
            SettingValue = "5",
            UpdatedAt = DateTime.UtcNow
        });

        context.Seasons.Add(new Season
        {
            Id = 1,
            Name = "2026 Q2",
            Year = 2026,
            Quarter = Quarter.Q2,
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    public static User CreateUser(GarageMagicDbContext context, string username = "TestUser", string email = "test@test.com")
    {
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            CurrentPrestigeLevel = 0,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    public static Deck CreateDeck(GarageMagicDbContext context, int userId, string name = "Test Deck")
    {
        var deck = new Deck
        {
            UserId = userId,
            DeckName = name,
            CommanderName = "Test Commander",
            ColorIdentity = "UB",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Decks.Add(deck);
        context.SaveChanges();
        return deck;
    }
}

