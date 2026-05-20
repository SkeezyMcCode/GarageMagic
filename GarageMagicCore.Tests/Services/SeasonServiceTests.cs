using FluentAssertions;
using GarageMagicCore.DTOs.Season;
using GarageMagicCore.Models;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;

namespace GarageMagicCore.Tests.Services;

public class SeasonServiceTests
{
    private SeasonService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new SeasonService(context);
    }

    // --- GetCurrent ---

    [Fact]
    public async Task GetCurrent_ReturnsActiveSeason()
    {
        var svc = CreateService(out _);
        var result = await svc.GetCurrentAsync();

        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        result.Name.Should().Be("2026 Q2");
    }

    [Fact]
    public async Task GetCurrent_NoActiveSeason_ReturnsNull()
    {
        var ctx = TestDbHelper.CreateContext();
        ctx.Seasons.First().IsActive = false;
        ctx.SaveChanges();
        var svc = new SeasonService(ctx);

        var result = await svc.GetCurrentAsync();
        result.Should().BeNull();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingSeason_ReturnsDto()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(999);
        result.Should().BeNull();
    }

    // --- GetAll ---

    [Fact]
    public async Task GetAll_ReturnsAllSeasons()
    {
        var svc = CreateService(out _);
        var result = await svc.GetAllAsync();
        result.Should().HaveCount(1);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidSeason_CreatesWithCorrectDates()
    {
        var svc = CreateService(out var ctx);

        var result = await svc.CreateAsync(new CreateSeasonDto
        {
            Name = "2026 Q3",
            Year = 2026,
            Quarter = Quarter.Q3
        });

        result.Should().NotBeNull();
        result.StartDate.Month.Should().Be(7);
        result.EndDate.Month.Should().Be(9);
        result.IsActive.Should().BeFalse(); // New season starts inactive
        ctx.Seasons.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_DuplicateYearQuarter_ThrowsInvalidOperation()
    {
        var svc = CreateService(out _);

        await svc.Invoking(s => s.CreateAsync(new CreateSeasonDto
        {
            Name = "2026 Q2 Duplicate",
            Year = 2026,
            Quarter = Quarter.Q2
        }))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*already exists*");
    }

    // --- Rollover ---

    [Fact]
    public async Task Rollover_AdvancesQuarterAndActivatesNewSeason()
    {
        var svc = CreateService(out var ctx);

        var next = await svc.RolloverAsync();

        next.Quarter.Should().Be(Quarter.Q3);
        next.Year.Should().Be(2026);
        next.IsActive.Should().BeTrue();

        // Previous season should be deactivated
        ctx.Seasons.Find(1)!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Rollover_Q4_WrapsToNextYearQ1()
    {
        var ctx = TestDbHelper.CreateContext();
        // Replace Q2 season with Q4
        var existing = ctx.Seasons.First();
        existing.Quarter = Quarter.Q4;
        existing.Year = 2026;
        ctx.SaveChanges();
        var svc = new SeasonService(ctx);

        var next = await svc.RolloverAsync();

        next.Quarter.Should().Be(Quarter.Q1);
        next.Year.Should().Be(2027);
    }

    [Fact]
    public async Task Rollover_NoActiveSeason_ThrowsInvalidOperation()
    {
        var ctx = TestDbHelper.CreateContext();
        ctx.Seasons.First().IsActive = false;
        ctx.SaveChanges();
        var svc = new SeasonService(ctx);

        await svc.Invoking(s => s.RolloverAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active season*");
    }

    // --- GetStandings ---

    [Fact]
    public async Task GetStandings_ReturnsOrderedByWins()
    {
        var ctx = TestDbHelper.CreateContext();
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");

        ctx.UserStats.AddRange(
            new GarageMagicCore.Models.UserStats { UserId = u1.Id, SeasonId = 1, TotalWins = 5, TotalLosses = 1, TotalMatches = 6, CreatedAt = DateTime.UtcNow },
            new GarageMagicCore.Models.UserStats { UserId = u2.Id, SeasonId = 1, TotalWins = 2, TotalLosses = 4, TotalMatches = 6, CreatedAt = DateTime.UtcNow }
        );
        ctx.SaveChanges();

        var svc = new SeasonService(ctx);
        var result = await svc.GetStandingsAsync(1);

        result.Should().NotBeNull();
        result!.Standings.Should().HaveCount(2);
        result.Standings[0].Username.Should().Be("A");
        result.Standings[0].TotalWins.Should().Be(5);
        result.Standings[1].Username.Should().Be("B");
    }

    [Fact]
    public async Task GetStandings_NonExistentSeason_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetStandingsAsync(999);
        result.Should().BeNull();
    }
}

