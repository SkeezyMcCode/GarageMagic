using FluentAssertions;
using GarageMagicCore.DTOs.User;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;

namespace GarageMagicCore.Tests.Services;

public class UserServiceTests
{
    private UserService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new UserService(context);
    }

    // --- Register ---

    [Fact]
    public async Task Register_ValidDto_CreatesUser()
    {
        var svc = CreateService(out var ctx);
        var dto = new CreateUserDto { Username = "Alice", Email = "alice@test.com", Password = "Password1" };

        var result = await svc.RegisterAsync(dto);

        result.Should().NotBeNull();
        result.Username.Should().Be("Alice");
        result.Email.Should().Be("alice@test.com");
        result.Id.Should().BeGreaterThan(0);
        ctx.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        TestDbHelper.CreateUser(ctx, "Alice", "alice@test.com");

        var dto = new CreateUserDto { Username = "Alice", Email = "other@test.com", Password = "Password1" };

        await svc.Invoking(s => s.RegisterAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alice*");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        TestDbHelper.CreateUser(ctx, "Alice", "alice@test.com");

        var dto = new CreateUserDto { Username = "Bob", Email = "alice@test.com", Password = "Password1" };

        await svc.Invoking(s => s.RegisterAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*alice@test.com*");
    }

    [Fact]
    public async Task Register_PasswordIsHashed()
    {
        var svc = CreateService(out var ctx);
        var dto = new CreateUserDto { Username = "Alice", Email = "alice@test.com", Password = "Password1" };

        await svc.RegisterAsync(dto);

        var user = ctx.Users.First();
        user.PasswordHash.Should().NotBe("Password1");
        BCrypt.Net.BCrypt.Verify("Password1", user.PasswordHash).Should().BeTrue();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingUser_ReturnsDto()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(999);
        result.Should().BeNull();
    }

    // --- Update ---

    [Fact]
    public async Task Update_ValidEmail_UpdatesUser()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.UpdateAsync(user.Id, new UpdateUserDto { Email = "new@test.com" });

        result.Should().NotBeNull();
        result!.Email.Should().Be("new@test.com");
        ctx.Users.Find(user.Id)!.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task Update_ValidPassword_HashesNewPassword()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        await svc.UpdateAsync(user.Id, new UpdateUserDto { Password = "NewPass1!" });

        var updated = ctx.Users.Find(user.Id)!;
        BCrypt.Net.BCrypt.Verify("NewPass1!", updated.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.UpdateAsync(999, new UpdateUserDto { Email = "x@x.com" });
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_DuplicateEmail_ThrowsInvalidOperation()
    {
        var svc = CreateService(out var ctx);
        TestDbHelper.CreateUser(ctx, "Alice", "alice@test.com");
        var bob = TestDbHelper.CreateUser(ctx, "Bob", "bob@test.com");

        await svc.Invoking(s => s.UpdateAsync(bob.Id, new UpdateUserDto { Email = "alice@test.com" }))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    // --- GetWithStats ---

    [Fact]
    public async Task GetWithStats_ReturnsAggregatedStats()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);
        ctx.UserStats.Add(new GarageMagicCore.Models.UserStats
        {
            UserId = user.Id, SeasonId = 1,
            TotalWins = 3, TotalLosses = 2, TotalMatches = 5,
            CreatedAt = DateTime.UtcNow
        });
        ctx.Decks.Add(new GarageMagicCore.Models.Deck
        {
            UserId = user.Id, DeckName = "D1", CommanderName = "Cmd",
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();

        var result = await svc.GetWithStatsAsync(user.Id);

        result.Should().NotBeNull();
        result!.TotalWins.Should().Be(3);
        result.TotalLosses.Should().Be(2);
        result.WinRate.Should().Be(60.00m);
        result.TotalDecks.Should().Be(1);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingUser_ReturnsTrue()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.DeleteAsync(user.Id);

        result.Should().BeTrue();
        ctx.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsFalse()
    {
        var svc = CreateService(out _);
        var result = await svc.DeleteAsync(999);
        result.Should().BeFalse();
    }
}

