using FluentAssertions;
using GarageMagicCore.DTOs.Match;
using GarageMagicCore.Models;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;
using MatchType = GarageMagicCore.Models.MatchType;

namespace GarageMagicCore.Tests.Services;

public class MatchServiceTests
{
    private MatchService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new MatchService(context);
    }

    private static CreateMatchDto Build1v1v1Match(int u1, int u2, int u3, int? d1, int? d2, int? d3, int winnerId) =>
        new()
        {
            MatchType = MatchType.OneVsOneVsOne,
            MatchDate = DateTime.UtcNow.AddHours(-1),
            Participants = new List<MatchParticipantDto>
            {
                new() { UserId = u1, DeckId = d1 },
                new() { UserId = u2, DeckId = d2 },
                new() { UserId = u3, DeckId = d3 }
            },
            WinnerUserIds = new List<int> { winnerId }
        };

    // --- Create ---

    [Fact]
    public async Task Create_1v1v1_RecordsMatchAndParticipants()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        var result = await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        result.Should().NotBeNull();
        result.Participants.Should().HaveCount(3);
        result.Winners.Should().HaveCount(1);
        result.Winners[0].UserId.Should().Be(u1.Id);
    }

    [Fact]
    public async Task Create_NoActiveSeason_ThrowsInvalidOperation()
    {
        var ctx = TestDbHelper.CreateContext();
        // Deactivate the seeded season
        var season = ctx.Seasons.First();
        season.IsActive = false;
        ctx.SaveChanges();
        var svc = new MatchService(ctx);

        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        await svc.Invoking(s => s.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active season*");
    }

    [Fact]
    public async Task Create_UpdatesWinnerStats()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        var stats = ctx.UserStats.First(s => s.UserId == u1.Id);
        stats.TotalWins.Should().Be(1);
        stats.TotalLosses.Should().Be(0);
        stats.TotalMatches.Should().Be(1);
        stats.Wins1v1v1.Should().Be(1);
    }

    [Fact]
    public async Task Create_UpdatesLoserStats()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        var stats = ctx.UserStats.First(s => s.UserId == u2.Id);
        stats.TotalWins.Should().Be(0);
        stats.TotalLosses.Should().Be(1);
        stats.TotalMatches.Should().Be(1);
    }

    [Fact]
    public async Task Create_WinnerDeckTracked_InWinsPerDeckJson()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");
        var deck = TestDbHelper.CreateDeck(ctx, u1.Id);

        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, deck.Id, null, null, u1.Id));

        var stats = ctx.UserStats.First(s => s.UserId == u1.Id);
        stats.WinsPerDeckJson.Should().Contain(deck.Id.ToString());
    }

    [Fact]
    public async Task Create_PrestigeTriggered_WhenWinsHitThreshold()
    {
        var ctx = TestDbHelper.CreateContext();
        // Set threshold to 2 for fast test
        ctx.AppSettings.First(s => s.SettingKey == GarageMagicCore.Models.SettingKeys.WinsPerPrestigeLevel).SettingValue = "2";
        ctx.SaveChanges();
        var svc = new MatchService(ctx);

        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        // Win twice
        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));
        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        var user = ctx.Users.Find(u1.Id)!;
        user.CurrentPrestigeLevel.Should().Be(1);
        ctx.PrestigeLevels.Should().ContainSingle(p => p.UserId == u1.Id && p.Level == 1);
    }

    [Fact]
    public async Task Create_PrestigeNotTriggered_BelowThreshold()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        // Default threshold is 5; 1 win shouldn't trigger it
        ctx.Users.Find(u1.Id)!.CurrentPrestigeLevel.Should().Be(0);
    }

    [Fact]
    public async Task Create_SheriffMatch_TracksRoleStats()
    {
        var svc = CreateService(out var ctx);
        var users = Enumerable.Range(1, 5)
            .Select(i => TestDbHelper.CreateUser(ctx, $"U{i}", $"u{i}@t.com"))
            .ToList();
        var decks = users.Select(u => TestDbHelper.CreateDeck(ctx, u.Id)).ToList();

        var dto = new CreateMatchDto
        {
            MatchType = MatchType.FivePlayerSheriff,
            MatchDate = DateTime.UtcNow.AddHours(-1),
            Participants = new List<MatchParticipantDto>
            {
                new() { UserId = users[0].Id, DeckId = decks[0].Id, HiddenRole = HiddenRole.Sheriff },
                new() { UserId = users[1].Id, DeckId = decks[1].Id, HiddenRole = HiddenRole.Deputy },
                new() { UserId = users[2].Id, DeckId = decks[2].Id, HiddenRole = HiddenRole.Red },
                new() { UserId = users[3].Id, DeckId = decks[3].Id, HiddenRole = HiddenRole.Red },
                new() { UserId = users[4].Id, DeckId = decks[4].Id, HiddenRole = HiddenRole.Red }
            },
            WinnerUserIds = new List<int> { users[0].Id, users[1].Id }
        };

        await svc.CreateAsync(dto);

        var sheriffStats = ctx.UserStats.First(s => s.UserId == users[0].Id);
        sheriffStats.SheriffGamesPlayed.Should().Be(1);
        sheriffStats.SheriffGamesWon.Should().Be(1);
        sheriffStats.WinsSheriff.Should().Be(1);

        var deputyStats = ctx.UserStats.First(s => s.UserId == users[1].Id);
        deputyStats.DeputyGamesPlayed.Should().Be(1);
        deputyStats.DeputyGamesWon.Should().Be(1);

        var redStats = ctx.UserStats.First(s => s.UserId == users[2].Id);
        redStats.RedGamesPlayed.Should().Be(1);
        redStats.RedGamesWon.Should().Be(0);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingMatch_ReturnsDto()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        var created = await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));
        var result = await svc.GetByIdAsync(created.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(999);
        result.Should().BeNull();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingMatch_ReturnsTrue()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");
        var match = await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));

        var result = await svc.DeleteAsync(match.Id);

        result.Should().BeTrue();
        ctx.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsFalse()
    {
        var svc = CreateService(out _);
        var result = await svc.DeleteAsync(999);
        result.Should().BeFalse();
    }

    // --- GetByUser ---

    [Fact]
    public async Task GetByUser_ReturnsOnlyUserMatches()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u1.Id));
        await svc.CreateAsync(Build1v1v1Match(u1.Id, u2.Id, u3.Id, null, null, null, u2.Id));

        var result = await svc.GetByUserAsync(u1.Id);
        result.Should().HaveCount(2); // u1 was in both

        var resultU3Only = await svc.GetByUserAsync(u3.Id);
        resultU3Only.Should().HaveCount(2); // u3 was also in both
    }
}


