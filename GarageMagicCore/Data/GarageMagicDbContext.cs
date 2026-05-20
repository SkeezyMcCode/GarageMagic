using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Models;
using GarageMagicCore.Data.EntityConfigurations;

namespace GarageMagicCore.Data;

public class GarageMagicDbContext : DbContext
{
    public GarageMagicDbContext(DbContextOptions<GarageMagicDbContext> options)
        : base(options)
    {
    }
    
    // DbSets for all entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchWinner> MatchWinners => Set<MatchWinner>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<UserStats> UserStats => Set<UserStats>();
    public DbSet<PrestigeLevel> PrestigeLevels => Set<PrestigeLevel>();
    public DbSet<Betrayal> Betrayals => Set<Betrayal>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DeckConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new MatchWinnerConfiguration());
        modelBuilder.ApplyConfiguration(new MatchParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new UserStatsConfiguration());
        modelBuilder.ApplyConfiguration(new PrestigeLevelConfiguration());
        modelBuilder.ApplyConfiguration(new BetrayalConfiguration());
        modelBuilder.ApplyConfiguration(new AppSettingsConfiguration());
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update timestamps
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added)
                    user.CreatedAt = DateTime.UtcNow;
                else
                    user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Deck deck)
            {
                if (entry.State == EntityState.Added)
                    deck.CreatedAt = DateTime.UtcNow;
                else
                    deck.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Match match)
            {
                if (entry.State == EntityState.Added)
                    match.CreatedAt = DateTime.UtcNow;
                else
                    match.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is UserStats stats)
            {
                if (entry.State == EntityState.Added)
                    stats.CreatedAt = DateTime.UtcNow;
                else
                    stats.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Season season)
            {
                if (entry.State == EntityState.Added)
                    season.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is MatchWinner winner)
            {
                if (entry.State == EntityState.Added)
                    winner.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is MatchParticipant participant)
            {
                if (entry.State == EntityState.Added)
                    participant.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Betrayal betrayal)
            {
                if (entry.State == EntityState.Added)
                    betrayal.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AppSettings settings)
            {
                settings.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}

