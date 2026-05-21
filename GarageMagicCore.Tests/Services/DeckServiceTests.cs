using FluentAssertions;
using GarageMagicCore.DTOs.Deck;
using GarageMagicCore.DTOs.Scryfall;
using GarageMagicCore.Services;
using GarageMagicCore.Tests.Helpers;

namespace GarageMagicCore.Tests.Services;

/// <summary>
/// No-op Scryfall stub — always returns null so deck tests stay fast and offline.
/// </summary>
file sealed class NullScryfallService : IScryfallService
{
    public Task<CommanderAutocompleteDto> AutocompleteCommanderAsync(string query, int limit = 20)
        => Task.FromResult(new CommanderAutocompleteDto());

    public Task<CommanderCardDto?> LookupCommanderAsync(string name)
        => Task.FromResult<CommanderCardDto?>(null);

    public Task<SymbologyDto?> GetSymbologyAsync()
        => Task.FromResult<SymbologyDto?>(new SymbologyDto());
}

public class DeckServiceTests
{
    private DeckService CreateService(out GarageMagicCore.Data.GarageMagicDbContext context)
    {
        context = TestDbHelper.CreateContext();
        return new DeckService(context, new NullScryfallService());
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidDto_CreatesDeck()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.CreateAsync(user.Id, new CreateDeckDto
        {
            DeckName = "Atraxa",
            CommanderName = "Atraxa, Praetors' Voice",
            ColorIdentity = "WUBG"
        });

        result.Should().NotBeNull();
        result.DeckName.Should().Be("Atraxa");
        result.UserId.Should().Be(user.Id);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_UnknownUser_ThrowsInvalidOperation()
    {
        var svc = CreateService(out _);

        await svc.Invoking(s => s.CreateAsync(999, new CreateDeckDto
        {
            DeckName = "X",
            CommanderName = "Y"
        }))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*999*");
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_ExistingDeck_ReturnsDto()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);
        var deck = TestDbHelper.CreateDeck(ctx, user.Id);

        var result = await svc.GetByIdAsync(deck.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(deck.Id);
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
    public async Task Update_ValidDto_UpdatesDeck()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);
        var deck = TestDbHelper.CreateDeck(ctx, user.Id, "Old Name");

        var result = await svc.UpdateAsync(deck.Id, new UpdateDeckDto { DeckName = "New Name", IsActive = false });

        result.Should().NotBeNull();
        result!.DeckName.Should().Be("New Name");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.UpdateAsync(999, new UpdateDeckDto { DeckName = "X" });
        result.Should().BeNull();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_ExistingDeck_SoftDeletesAndReturnsTrue()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);
        var deck = TestDbHelper.CreateDeck(ctx, user.Id);

        var result = await svc.DeleteAsync(deck.Id);

        result.Should().BeTrue();
        ctx.Decks.Find(deck.Id)!.IsActive.Should().BeFalse();
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
    public async Task GetByUser_ReturnsOnlyUserDecks()
    {
        var svc = CreateService(out var ctx);
        var user1 = TestDbHelper.CreateUser(ctx, "User1", "u1@test.com");
        var user2 = TestDbHelper.CreateUser(ctx, "User2", "u2@test.com");
        TestDbHelper.CreateDeck(ctx, user1.Id, "Deck A");
        TestDbHelper.CreateDeck(ctx, user1.Id, "Deck B");
        TestDbHelper.CreateDeck(ctx, user2.Id, "Deck C");

        var result = await svc.GetByUserAsync(user1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.UserId.Should().Be(user1.Id));
    }

    [Fact]
    public async Task GetByUser_NoDecks_ReturnsEmpty()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);

        var result = await svc.GetByUserAsync(user.Id);

        result.Should().BeEmpty();
    }

    // --- GetWithStats ---

    [Fact]
    public async Task GetWithStats_NoPriorMatches_ReturnsZeroStats()
    {
        var svc = CreateService(out var ctx);
        var user = TestDbHelper.CreateUser(ctx);
        var deck = TestDbHelper.CreateDeck(ctx, user.Id);

        var result = await svc.GetWithStatsAsync(deck.Id);

        result.Should().NotBeNull();
        result!.Wins.Should().Be(0);
        result.TotalMatches.Should().Be(0);
        result.WinRate.Should().Be(0);
    }
}

