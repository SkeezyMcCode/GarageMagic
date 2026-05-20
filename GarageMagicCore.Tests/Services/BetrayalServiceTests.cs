using FluentAssertions;
using GarageMagicCore.DTOs.Betrayal;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;

namespace GarageMagicCore.Tests.Services;

public class BetrayalServiceTests
{
    private BetrayalService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new BetrayalService(context);
    }

    // --- Create ---

    [Fact]
    public async Task Create_Valid_RecordsBetrayalWithUsernames()
    {
        var svc = CreateService(out var ctx);
        var betrayer = TestDbHelper.CreateUser(ctx, "Judas", "judas@test.com");
        var victim = TestDbHelper.CreateUser(ctx, "Caesar", "caesar@test.com");

        var result = await svc.CreateAsync(new CreateBetrayalDto
        {
            BetrayerUserId = betrayer.Id,
            VictimUserId = victim.Id,
            Description = "Stabbed in the back on turn 7",
            BetrayalDate = DateTime.UtcNow.AddHours(-1)
        });

        result.Should().NotBeNull();
        result.BetrayerUsername.Should().Be("Judas");
        result.VictimUsername.Should().Be("Caesar");
        result.Description.Should().Be("Stabbed in the back on turn 7");
        ctx.Betrayals.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_BetrayerNotFound_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        var victim = TestDbHelper.CreateUser(ctx, "Caesar", "caesar@test.com");

        await svc.Invoking(s => s.CreateAsync(new CreateBetrayalDto
        {
            BetrayerUserId = 999,
            VictimUserId = victim.Id,
            Description = "Test",
            BetrayalDate = DateTime.UtcNow
        }))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*999*");
    }

    [Fact]
    public async Task Create_VictimNotFound_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        var betrayer = TestDbHelper.CreateUser(ctx, "Judas", "judas@test.com");

        await svc.Invoking(s => s.CreateAsync(new CreateBetrayalDto
        {
            BetrayerUserId = betrayer.Id,
            VictimUserId = 999,
            Description = "Test",
            BetrayalDate = DateTime.UtcNow
        }))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*999*");
    }

    [Fact]
    public async Task Create_SameUser_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        await svc.Invoking(s => s.CreateAsync(new CreateBetrayalDto
        {
            BetrayerUserId = user.Id,
            VictimUserId = user.Id,
            Description = "Self-betrayal",
            BetrayalDate = DateTime.UtcNow
        }))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*cannot betray themselves*");
    }

    [Fact]
    public async Task Create_DefaultDate_UsesUtcNow()
    {
        var svc = CreateService(out var ctx);
        var betrayer = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var victim = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var before = DateTime.UtcNow;

        var result = await svc.CreateAsync(new CreateBetrayalDto
        {
            BetrayerUserId = betrayer.Id,
            VictimUserId = victim.Id,
            Description = "Test",
            BetrayalDate = default
        });

        result.BetrayalDate.Should().BeOnOrAfter(before);
    }

    // --- GetByUser ---

    [Fact]
    public async Task GetByUser_ReturnsAsBothBetrayerAndVictim()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");
        var u3 = TestDbHelper.CreateUser(ctx, "C", "c@t.com");

        // u1 betrays u2
        await svc.CreateAsync(new CreateBetrayalDto { BetrayerUserId = u1.Id, VictimUserId = u2.Id, Description = "1->2", BetrayalDate = DateTime.UtcNow });
        // u3 betrays u1
        await svc.CreateAsync(new CreateBetrayalDto { BetrayerUserId = u3.Id, VictimUserId = u1.Id, Description = "3->1", BetrayalDate = DateTime.UtcNow });
        // u2 betrays u3 (u1 not involved)
        await svc.CreateAsync(new CreateBetrayalDto { BetrayerUserId = u2.Id, VictimUserId = u3.Id, Description = "2->3", BetrayalDate = DateTime.UtcNow });

        var result = await svc.GetByUserAsync(u1.Id);

        result.Should().HaveCount(2); // u1 betrays u2 + u3 betrays u1
    }

    [Fact]
    public async Task GetByUser_NoBetrayals_ReturnsEmpty()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.GetByUserAsync(user.Id);
        result.Should().BeEmpty();
    }

    // --- GetRecent ---

    [Fact]
    public async Task GetRecent_ReturnsLatestFirst_LimitedByCount()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");

        for (var i = 1; i <= 5; i++)
        {
            await svc.CreateAsync(new CreateBetrayalDto
            {
                BetrayerUserId = u1.Id,
                VictimUserId = u2.Id,
                Description = $"Betrayal {i}",
                BetrayalDate = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var result = await svc.GetRecentAsync(count: 3);

        result.Should().HaveCount(3);
        // Most recent first
        result[0].Description.Should().Be("Betrayal 1");
    }

    [Fact]
    public async Task GetRecent_FewerThanCount_ReturnsAll()
    {
        var svc = CreateService(out var ctx);
        var u1 = TestDbHelper.CreateUser(ctx, "A", "a@t.com");
        var u2 = TestDbHelper.CreateUser(ctx, "B", "b@t.com");

        await svc.CreateAsync(new CreateBetrayalDto { BetrayerUserId = u1.Id, VictimUserId = u2.Id, Description = "Only one", BetrayalDate = DateTime.UtcNow });

        var result = await svc.GetRecentAsync(count: 10);
        result.Should().HaveCount(1);
    }
}

