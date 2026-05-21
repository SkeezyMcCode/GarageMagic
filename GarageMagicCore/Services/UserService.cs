using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.DTOs.User;
using GarageMagicCore.Models;

namespace GarageMagicCore.Services;

public class UserService : IUserService
{
    private readonly GarageMagicDbContext _context;

    public UserService(GarageMagicDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> RegisterAsync(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsApproved = false,
            IsAdmin = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<List<UserDto>> GetSelectableAsync()
    {
        // Returns all players valid for match participation:
        // approved regular users + guests, excluding soft-deleted merged accounts
        return await _context.Users
            .Where(u => u.IsApproved && !u.Username.StartsWith("__merged_"))
            .OrderBy(u => u.IsGuest)   // real players first, guests after
            .ThenBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CurrentPrestigeLevel = u.CurrentPrestigeLevel,
                CreatedAt = u.CreatedAt,
                IsApproved = u.IsApproved,
                IsAdmin = u.IsAdmin,
                IsGuest = u.IsGuest
            })
            .ToListAsync();
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");
            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _context.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserWithStatsDto?> GetWithStatsAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Decks)
            .Include(u => u.Stats)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return null;

        var totalWins = user.Stats.Sum(s => s.TotalWins);
        var totalLosses = user.Stats.Sum(s => s.TotalLosses);
        var totalMatches = totalWins + totalLosses;

        return new UserWithStatsDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CurrentPrestigeLevel = user.CurrentPrestigeLevel,
            CreatedAt = user.CreatedAt,
            TotalDecks = user.Decks.Count,
            TotalWins = totalWins,
            TotalLosses = totalLosses,
            WinRate = totalMatches > 0 ? Math.Round((decimal)totalWins / totalMatches * 100, 2) : 0
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PendingUserDto>> GetPendingAsync()
    {
        return await _context.Users
            .Where(u => !u.IsApproved)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new PendingUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserDto?> ApproveAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.IsApproved = true;
        await _context.SaveChangesAsync();

        // Create UserStats for the active season so the player appears in the leaderboard immediately
        var activeSeason = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
        if (activeSeason != null)
        {
            var statsExist = await _context.UserStats
                .AnyAsync(s => s.UserId == id && s.SeasonId == activeSeason.Id);
            if (!statsExist)
            {
                _context.UserStats.Add(new Models.UserStats
                {
                    UserId = id,
                    SeasonId = activeSeason.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        return MapToDto(user);
    }

    public async Task<bool> RejectAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto> CreateGuestAsync(CreateGuestDto dto)
    {
        var name = dto.DisplayName.Trim();
        // Ensure unique username — append a number if taken
        var username = name;
        var suffix = 1;
        while (await _context.Users.AnyAsync(u => u.Username == username))
            username = $"{name}{++suffix}";

        var guest = new User
        {
            Username = username,
            Email = $"guest_{Guid.NewGuid():N}@guest.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            IsApproved = true,
            IsAdmin = false,
            IsGuest = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(guest);
        await _context.SaveChangesAsync();
        return MapToDto(guest);
    }

    public async Task<List<UserDto>> GetGuestsAsync()
    {
        return await _context.Users
            .Where(u => u.IsGuest)
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id, Username = u.Username, Email = u.Email,
                CurrentPrestigeLevel = u.CurrentPrestigeLevel,
                CreatedAt = u.CreatedAt, IsApproved = u.IsApproved,
                IsAdmin = u.IsAdmin, IsGuest = u.IsGuest
            })
            .ToListAsync();
    }

    public async Task<UserDto> SetAdminAsync(int id, bool isAdmin)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.IsAdmin   = isAdmin;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserDto> ApproveAndLinkAsync(int pendingUserId, int guestUserId)
    {
        // ── Fast-fail validation (outside transaction) ────────────────────────
        if (pendingUserId == guestUserId)
            throw new ArgumentException("Cannot link a user to themselves.");

        var pendingUser = await _context.Users.FindAsync(pendingUserId)
            ?? throw new KeyNotFoundException($"Pending user {pendingUserId} not found.");

        if (pendingUser.IsApproved)
            throw new InvalidOperationException($"User {pendingUserId} is already approved.");

        var guestUser = await _context.Users.FindAsync(guestUserId)
            ?? throw new KeyNotFoundException($"Guest user {guestUserId} not found.");

        if (!guestUser.IsGuest)
            throw new InvalidOperationException($"User {guestUserId} is not a guest.");

        // ── Transaction ───────────────────────────────────────────────────────
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Resolve username conflict (edge case: someone already has that username)
            if (await _context.Users.AnyAsync(u => u.Username == pendingUser.Username && u.Id != pendingUser.Id))
            {
                var baseUsername = pendingUser.Username;
                var suffix = 1;
                while (await _context.Users.AnyAsync(u => u.Username == $"{baseUsername}_{suffix}" && u.Id != pendingUser.Id))
                    suffix++;
                pendingUser.Username = $"{baseUsername}_{suffix}";
            }

            // 2. Approve the pending user
            pendingUser.IsApproved = true;
            pendingUser.UpdatedAt  = DateTime.UtcNow;

            // 3. Migrate MatchParticipants
            var participants = await _context.MatchParticipants
                .Where(mp => mp.UserId == guestUserId).ToListAsync();
            foreach (var mp in participants) mp.UserId = pendingUserId;

            // 4. Migrate MatchWinners
            var winners = await _context.MatchWinners
                .Where(mw => mw.UserId == guestUserId).ToListAsync();
            foreach (var mw in winners) mw.UserId = pendingUserId;

            // 5. Migrate Betrayals (both betrayer and victim roles)
            var betrayalsAsBetrayer = await _context.Betrayals
                .Where(b => b.BetrayerUserId == guestUserId).ToListAsync();
            foreach (var b in betrayalsAsBetrayer) b.BetrayerUserId = pendingUserId;

            var betrayalsAsVictim = await _context.Betrayals
                .Where(b => b.VictimUserId == guestUserId).ToListAsync();
            foreach (var b in betrayalsAsVictim) b.VictimUserId = pendingUserId;

            // 6. Migrate Decks
            var decks = await _context.Decks
                .Where(d => d.UserId == guestUserId).ToListAsync();
            foreach (var d in decks) d.UserId = pendingUserId;

            // 7. Migrate Match.SheriffUserId references
            var sheriffMatches = await _context.Matches
                .Where(m => m.SheriffUserId == guestUserId).ToListAsync();
            foreach (var m in sheriffMatches) m.SheriffUserId = pendingUserId;

            // 8. Migrate PrestigeLevels
            var prestigeLevels = await _context.PrestigeLevels
                .Where(p => p.UserId == guestUserId).ToListAsync();
            foreach (var p in prestigeLevels) p.UserId = pendingUserId;


            // 9. Migrate UserStats — merge into pending user's row if season already has one
            var guestStats = await _context.UserStats
                .Where(s => s.UserId == guestUserId)
                .ToListAsync();

            foreach (var gs in guestStats)
            {
                var existing = await _context.UserStats
                    .FirstOrDefaultAsync(s => s.UserId == pendingUserId && s.SeasonId == gs.SeasonId);

                if (existing == null)
                {
                    gs.UserId = pendingUserId;
                }
                else
                {
                    existing.TotalWins          += gs.TotalWins;
                    existing.TotalLosses        += gs.TotalLosses;
                    existing.TotalMatches       += gs.TotalMatches;
                    existing.Wins1v1v1          += gs.Wins1v1v1;
                    existing.Wins1v1v1v1        += gs.Wins1v1v1v1;
                    existing.WinsSheriff        += gs.WinsSheriff;
                    existing.SheriffGamesPlayed += gs.SheriffGamesPlayed;
                    existing.SheriffGamesWon    += gs.SheriffGamesWon;
                    existing.DeputyGamesPlayed  += gs.DeputyGamesPlayed;
                    existing.DeputyGamesWon     += gs.DeputyGamesWon;
                    existing.OutlawGamesPlayed  += gs.OutlawGamesPlayed;
                    existing.OutlawGamesWon     += gs.OutlawGamesWon;
                    existing.RenegadeGamesPlayed += gs.RenegadeGamesPlayed;
                    existing.RenegadeGamesWon   += gs.RenegadeGamesWon;
                    existing.MatriarchTriggered += gs.MatriarchTriggered;
                    existing.MatriarchWins      += gs.MatriarchWins;
                    existing.UpdatedAt           = DateTime.UtcNow;
                    _context.UserStats.Remove(gs);
                }
            }

            // 10. Ensure a UserStats row exists for the active season
            var activeSeason = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
            if (activeSeason != null)
            {
                var statsExist = await _context.UserStats
                    .AnyAsync(s => s.UserId == pendingUserId && s.SeasonId == activeSeason.Id);
                if (!statsExist)
                {
                    _context.UserStats.Add(new Models.UserStats
                    {
                        UserId    = pendingUserId,
                        SeasonId  = activeSeason.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // 11. Save tracked changes (stats merges, approve flag, username fix)
            await _context.SaveChangesAsync();

            // 12. Soft-deactivate the guest record to preserve any residual FK integrity
            guestUser.IsGuest    = false;
            guestUser.IsApproved = false;
            guestUser.Username   = $"__merged_{guestUser.Id}_{guestUser.Username}";
            guestUser.Email      = $"__merged_{guestUser.Id}@merged.local";
            guestUser.UpdatedAt  = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            return MapToDto(pendingUser);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        CurrentPrestigeLevel = user.CurrentPrestigeLevel,
        CreatedAt = user.CreatedAt,
        IsApproved = user.IsApproved,
        IsAdmin = user.IsAdmin,
        IsGuest = user.IsGuest
    };
}
