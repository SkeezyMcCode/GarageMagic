using FluentAssertions;
using GarageMagicCore.Models;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;
using System.Text.Json;

namespace GarageMagicCore.Tests.Services;

public class StatsServiceTests
{
    private StatsService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new StatsService(context);
    }

    // --- GetLeaderboard ---

    [Fact]
    public async Task GetLeaderboard_DefaultsToActiveSeason_OrderedByWins()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        ctx.UserStats.AddRange(
            new UserStats { UserId = u1.Id, SeasonId = 1, TotalWins = 10, TotalMatches = 12, TotalLosses = 2, CreatedAt = DateTime.UtcNow },
            new UserStats { UserId = u2.Id, SeasonId = 1, TotalWins = 4, TotalMatches = 8, TotalLosses = 4, CreatedAt = DateTime.UtcNow },
            new UserStats { UserId = u3.Id, SeasonId = 1, TotalWins = 7, TotalMatches = 10, TotalLosses = 3, CreatedAt = DateTime.UtcNow }
        );
        ctx.SaveChanges();

        var result = await svc.GetLeaderboardAsync();

        result.Should().HaveCount(3);
        result[0].Username.Should().Be("A"); // 10 wins
        result[1].Username.Should().Be("C"); // 7 wins
        result[2].Username.Should().Be("B"); // 4 wins
    }

    [Fact]
    public async Task GetLeaderboard_BySpecificSeason_FiltersCorrectly()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");

        ctx.UserStats.Add(new UserStats { UserId = u1.Id, SeasonId = 1, TotalWins = 3, TotalMatches = 5, TotalLosses = 2, CreatedAt = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await svc.GetLeaderboardAsync(seasonId: 1);
        result.Should().HaveCount(1);
        result[0].TotalWins.Should().Be(3);
    }

    [Fact]
    public async Task GetLeaderboard_NoActiveSeason_ReturnsEmpty()
    {
        var ctx = TestDbHelper.CreateContext();
        ctx.Seasons.First().IsActive = false;
        ctx.SaveChanges();
        var svc = new StatsService(ctx);

        var result = await svc.GetLeaderboardAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLeaderboard_WinRateCalculatedCorrectly()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");

        ctx.UserStats.Add(new UserStats { UserId = u1.Id, SeasonId = 1, TotalWins = 3, TotalMatches = 4, TotalLosses = 1, CreatedAt = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await svc.GetLeaderboardAsync();
        result[0].WinRate.Should().Be(75.00m);
    }

    // --- GetUserStats ---

    [Fact]
    public async Task GetUserStats_ReturnsFullBreakdown()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");

        ctx.UserStats.Add(new UserStats
        {
            UserId = u1.Id, SeasonId = 1,
            TotalWins = 6, TotalLosses = 2, TotalMatches = 8,
            Wins1v1v1 = 3, Wins1v1v1v1 = 2, WinsSheriff = 1,
            SheriffGamesPlayed = 2, SheriffGamesWon = 1,
            DeputyGamesPlayed = 1, DeputyGamesWon = 0,
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        var result = await svc.GetUserStatsAsync(u1.Id, 1);

        result.Should().NotBeNull();
        result!.TotalWins.Should().Be(6);
        result.Wins1v1v1.Should().Be(3);
        result.WinsSheriff.Should().Be(1);
        result.SheriffGamesPlayed.Should().Be(2);
        result.SheriffGamesWon.Should().Be(1);
        result.WinRate.Should().Be(75.00m);
        result.SeasonName.Should().Be("2026 Q2");
    }

    [Fact]
    public async Task GetUserStats_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetUserStatsAsync(999, 1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserStats_ZeroMatches_WinRateIsZero()
    {
        var svc = CreateService(out var ctx);
        var u = TestDbHelper.CreateUser(ctx);

        ctx.UserStats.Add(new UserStats { UserId = u.Id, SeasonId = 1, TotalMatches = 0, TotalWins = 0, TotalLosses = 0, CreatedAt = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await svc.GetUserStatsAsync(u.Id, 1);
        result!.WinRate.Should().Be(0);
    }

    // --- GetDeckStats ---

    [Fact]
    public async Task GetDeckStats_ReturnsWinsFromJson_OrderedByWins()
    {
        var svc = CreateService(out var ctx);
        var u = TestDbHelper.CreateUser(ctx);
        var d1 = TestDbHelper.CreateDeck(ctx, u.Id, "Deck A");
        var d2 = TestDbHelper.CreateDeck(ctx, u.Id, "Deck B");

        var deckWins = new Dictionary<int, int> { { d1.Id, 5 }, { d2.Id, 2 } };
        ctx.UserStats.Add(new UserStats
        {
            UserId = u.Id,
            SeasonId = 1,
            WinsPerDeckJson = JsonSerializer.Serialize(deckWins),
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        var result = await svc.GetDeckStatsAsync(u.Id, 1);

        result.Should().HaveCount(2);
        result[0].DeckName.Should().Be("Deck A");
        result[0].TotalWins.Should().Be(5);
        result[1].DeckName.Should().Be("Deck B");
        result[1].TotalWins.Should().Be(2);
    }

    [Fact]
    public async Task GetDeckStats_NullJson_ReturnsEmpty()
    {
        var svc = CreateService(out var ctx);
        var u = TestDbHelper.CreateUser(ctx);

        ctx.UserStats.Add(new UserStats { UserId = u.Id, SeasonId = 1, WinsPerDeckJson = null, CreatedAt = DateTime.UtcNow });
        ctx.SaveChanges();

        var result = await svc.GetDeckStatsAsync(u.Id, 1);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeckStats_NoStats_ReturnsEmpty()
    {
        var svc = CreateService(out var ctx);
        var u = TestDbHelper.CreateUser(ctx);

        var result = await svc.GetDeckStatsAsync(u.Id, 1);
        result.Should().BeEmpty();
    }
}

