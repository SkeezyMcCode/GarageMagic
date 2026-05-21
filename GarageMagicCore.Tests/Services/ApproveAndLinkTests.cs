using FluentAssertions;
using GarageMagicCore.Models;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using ModelMatchType = GarageMagicCore.Models.MatchType;

namespace GarageMagicCore.Tests.Services;

/// <summary>
/// Tests for UserService.ApproveAndLinkAsync —
/// approve a pending user and migrate all guest history in one transaction.
/// Note: EF Core InMemory silently ignores Begin/Commit/Rollback, so transaction
/// rollback is validated via early-exit guard clauses rather than DB-level rollback.
/// </summary>
public class ApproveAndLinkTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (UserService svc, GarageMagicCore.Data.GarageMagicDbContext ctx) Create()
    {
        var ctx = TestDbHelper.CreateContext();
        return (new UserService(ctx), ctx);
    }

    /// <summary>Creates an unapproved (pending) user.</summary>
    private static GarageMagicCore.Models.User AddPendingUser(
        GarageMagicCore.Data.GarageMagicDbContext ctx,
        string username = "PendingAlice",
        string email    = "alice@pending.com")
    {
        var u = new GarageMagicCore.Models.User
        {
            Username     = username,
            Email        = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1"),
            IsApproved   = false,
            IsGuest      = false,
            CreatedAt    = DateTime.UtcNow
        };
        ctx.Users.Add(u);
        ctx.SaveChanges();
        return u;
    }

    /// <summary>Creates an approved guest user.</summary>
    private static GarageMagicCore.Models.User AddGuestUser(
        GarageMagicCore.Data.GarageMagicDbContext ctx,
        string username = "GuestAlice")
    {
        var u = new GarageMagicCore.Models.User
        {
            Username     = username,
            Email        = $"guest_{Guid.NewGuid():N}@guest.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            IsApproved   = true,
            IsGuest      = true,
            CreatedAt    = DateTime.UtcNow
        };
        ctx.Users.Add(u);
        ctx.SaveChanges();
        return u;
    }

    // ── Validation guard tests ────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAndLink_SelfLink_ThrowsArgumentException()
    {
        var (svc, _) = Create();

        await svc.Invoking(s => s.ApproveAndLinkAsync(42, 42))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*themselves*");
    }

    [Fact]
    public async Task ApproveAndLink_PendingUserNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, _) = Create();

        await svc.Invoking(s => s.ApproveAndLinkAsync(9999, 1))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*9999*");
    }

    [Fact]
    public async Task ApproveAndLink_PendingUserAlreadyApproved_ThrowsInvalidOperation()
    {
        var (svc, ctx) = Create();
        var approvedUser = TestDbHelper.CreateUser(ctx, "ApprovedBob", "bob@test.com");
        approvedUser.IsApproved = true;
        ctx.SaveChanges();

        var guest = AddGuestUser(ctx);

        await svc.Invoking(s => s.ApproveAndLinkAsync(approvedUser.Id, guest.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already approved*");
    }

    [Fact]
    public async Task ApproveAndLink_GuestUserNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);

        await svc.Invoking(s => s.ApproveAndLinkAsync(pending.Id, 9999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*9999*");
    }

    [Fact]
    public async Task ApproveAndLink_TargetUserIsNotGuest_ThrowsInvalidOperation()
    {
        var (svc, ctx) = Create();
        var pending     = AddPendingUser(ctx);
        var regularUser = TestDbHelper.CreateUser(ctx, "RegularCarol", "carol@test.com");

        await svc.Invoking(s => s.ApproveAndLinkAsync(pending.Id, regularUser.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a guest*");
    }

    // ── Successful migration tests ────────────────────────────────────────────

    [Fact]
    public async Task ApproveAndLink_ApprovesThePendingUser()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        var result = await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        result.IsApproved.Should().BeTrue();
        result.Username.Should().Be(pending.Username);
        result.Id.Should().Be(pending.Id);

        var dbUser = await ctx.Users.FindAsync(pending.Id);
        dbUser!.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAndLink_MigratesMatchParticipants()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        // Create a match with the guest as participant
        var match = new Match
        {
            MatchType = ModelMatchType.OneVsOneVsOne,
            MatchDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Matches.Add(match);
        ctx.SaveChanges();

        ctx.MatchParticipants.Add(new MatchParticipant
        {
            MatchId   = match.Id,
            UserId    = guest.Id,
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var participant = await ctx.MatchParticipants.FirstAsync(mp => mp.MatchId == match.Id);
        participant.UserId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAndLink_MigratesMatchWinners()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        var match = new Match
        {
            MatchType = ModelMatchType.OneVsOneVsOne,
            MatchDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Matches.Add(match);
        ctx.SaveChanges();

        ctx.MatchWinners.Add(new MatchWinner
        {
            MatchId   = match.Id,
            UserId    = guest.Id,
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var winner = await ctx.MatchWinners.FirstAsync(mw => mw.MatchId == match.Id);
        winner.UserId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAndLink_MigratesBetrayals_BothRoles()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);
        var other   = TestDbHelper.CreateUser(ctx, "OtherDave", "dave@test.com");

        // Guest as betrayer
        ctx.Betrayals.Add(new Betrayal
        {
            BetrayerUserId = guest.Id,
            VictimUserId   = other.Id,
            Description    = "Stabbed in the back",
            BetrayalDate   = DateTime.UtcNow,
            CreatedAt      = DateTime.UtcNow
        });
        // Guest as victim
        ctx.Betrayals.Add(new Betrayal
        {
            BetrayerUserId = other.Id,
            VictimUserId   = guest.Id,
            Description    = "Rightful vengeance",
            BetrayalDate   = DateTime.UtcNow,
            CreatedAt      = DateTime.UtcNow
        });
        ctx.SaveChanges();

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var betrayals = await ctx.Betrayals.ToListAsync();
        betrayals.Should().HaveCount(2);
        betrayals.Should().AllSatisfy(b =>
        {
            b.BetrayerUserId.Should().NotBe(guest.Id);
            b.VictimUserId.Should().NotBe(guest.Id);
        });

        var asBetrayer = betrayals.First(b => b.Description == "Stabbed in the back");
        asBetrayer.BetrayerUserId.Should().Be(pending.Id);

        var asVictim = betrayals.First(b => b.Description == "Rightful vengeance");
        asVictim.VictimUserId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAndLink_MigratesDecks()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        TestDbHelper.CreateDeck(ctx, guest.Id, "Guest Deck");

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var deck = await ctx.Decks.FirstAsync();
        deck.UserId.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ApproveAndLink_MigratesUserStats_SimpleReassign()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        // Add a season the pending user has NO stats for
        var otherSeason = new Season
        {
            Name      = "2025 Q4",
            Year      = 2025,
            Quarter   = Quarter.Q4,
            StartDate = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate   = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            IsActive  = false,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Seasons.Add(otherSeason);
        ctx.SaveChanges();

        ctx.UserStats.Add(new GarageMagicCore.Models.UserStats
        {
            UserId       = guest.Id,
            SeasonId     = otherSeason.Id,
            TotalWins    = 7,
            TotalLosses  = 3,
            TotalMatches = 10,
            CreatedAt    = DateTime.UtcNow
        });
        ctx.SaveChanges();

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var migratedStats = await ctx.UserStats
            .FirstOrDefaultAsync(s => s.UserId == pending.Id && s.SeasonId == otherSeason.Id);

        migratedStats.Should().NotBeNull();
        migratedStats!.TotalWins.Should().Be(7);
        migratedStats.TotalLosses.Should().Be(3);
    }

    [Fact]
    public async Task ApproveAndLink_MergesUserStats_WhenSeasonRowAlreadyExists()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        // Both users already have stats for the active season (id=1)
        ctx.UserStats.Add(new GarageMagicCore.Models.UserStats
        {
            UserId = pending.Id, SeasonId = 1,
            TotalWins = 2, TotalLosses = 1, TotalMatches = 3,
            CreatedAt = DateTime.UtcNow
        });
        ctx.UserStats.Add(new GarageMagicCore.Models.UserStats
        {
            UserId = guest.Id, SeasonId = 1,
            TotalWins = 5, TotalLosses = 2, TotalMatches = 7,
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var merged = await ctx.UserStats
            .FirstAsync(s => s.UserId == pending.Id && s.SeasonId == 1);

        merged.TotalWins.Should().Be(7);    // 2 + 5
        merged.TotalLosses.Should().Be(3);  // 1 + 2
        merged.TotalMatches.Should().Be(10); // 3 + 7

        // Guest's row for this season should be gone
        var guestRow = await ctx.UserStats
            .FirstOrDefaultAsync(s => s.UserId == guest.Id && s.SeasonId == 1);
        guestRow.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAndLink_CreatesActiveSeasonStats_WhenMissing()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        // Neither user has stats for the active season
        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var activeStats = await ctx.UserStats
            .FirstOrDefaultAsync(s => s.UserId == pending.Id && s.SeasonId == 1);

        activeStats.Should().NotBeNull("approved users should auto-get an active-season stats row");
    }

    [Fact]
    public async Task ApproveAndLink_SoftDeactivatesGuestRecord()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);
        var guest   = AddGuestUser(ctx);

        await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        var guestAfter = await ctx.Users.FindAsync(guest.Id);
        guestAfter.Should().NotBeNull("guest record is preserved for referential integrity");
        guestAfter!.IsGuest.Should().BeFalse();
        guestAfter.IsApproved.Should().BeFalse();
        guestAfter.Username.Should().StartWith("__merged_");
        guestAfter.Email.Should().EndWith("@merged.local");
    }

    [Fact]
    public async Task ApproveAndLink_ReturnsCorrectUserDto()
    {
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx, "FinalAlice", "finalice@test.com");
        var guest   = AddGuestUser(ctx);

        var result = await svc.ApproveAndLinkAsync(pending.Id, guest.Id);

        result.Id.Should().Be(pending.Id);
        result.Username.Should().Be("FinalAlice");
        result.Email.Should().Be("finalice@test.com");
        result.IsApproved.Should().BeTrue();
        result.IsGuest.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveAndLink_EarlyValidationFails_LeavesDataUnchanged()
    {
        // Tests that guard clause failures (before any writes) leave db untouched.
        // Note: InMemory ignores transaction rollback semantics, so this validates
        // the early-exit guard approach rather than DB-level rollback.
        var (svc, ctx) = Create();
        var pending = AddPendingUser(ctx);

        // Pass a non-guest user — should throw before any mutations
        var notGuest = TestDbHelper.CreateUser(ctx, "NotAGuest", "notguest@test.com");

        var pendingBefore = await ctx.Users.FindAsync(pending.Id);
        var approvedBefore = pendingBefore!.IsApproved;

        await svc.Invoking(s => s.ApproveAndLinkAsync(pending.Id, notGuest.Id))
            .Should().ThrowAsync<InvalidOperationException>();

        var pendingAfter = await ctx.Users.FindAsync(pending.Id);
        pendingAfter!.IsApproved.Should().Be(approvedBefore, "no changes should be committed after a guard-clause failure");
    }
}




