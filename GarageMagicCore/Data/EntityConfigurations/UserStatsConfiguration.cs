using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class UserStatsConfiguration : IEntityTypeConfiguration<UserStats>
{
    public void Configure(EntityTypeBuilder<UserStats> builder)
    {
        builder.ToTable("UserStats");
        
        builder.HasKey(us => us.Id);
        
        builder.Property(us => us.TotalWins)
            .HasDefaultValue(0);
        
        builder.Property(us => us.TotalLosses)
            .HasDefaultValue(0);
        
        builder.Property(us => us.TotalMatches)
            .HasDefaultValue(0);
        
        builder.Property(us => us.Wins1v1v1)
            .HasDefaultValue(0);
        
        builder.Property(us => us.Wins1v1v1v1)
            .HasDefaultValue(0);
        
        builder.Property(us => us.WinsSheriff)
            .HasDefaultValue(0);
        
        builder.Property(us => us.SheriffGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.SheriffGamesWon)
            .HasDefaultValue(0);
        
        builder.Property(us => us.DeputyGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.DeputyGamesWon)
            .HasDefaultValue(0);
        
        builder.Property(us => us.RedGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.RedGamesWon)
            .HasDefaultValue(0);
        
        builder.Property(us => us.WinsPerDeckJson)
            .HasColumnType("TEXT");
        
        builder.Property(us => us.CreatedAt)
            .IsRequired();
        
        // Composite index - one UserStats per User per Season
        builder.HasIndex(us => new { us.UserId, us.SeasonId })
            .IsUnique();
    }
}

