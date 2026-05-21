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
        
        builder.Property(us => us.OutlawGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.OutlawGamesWon)
            .HasDefaultValue(0);
        
        builder.Property(us => us.RenegadeGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.RenegadeGamesWon)
            .HasDefaultValue(0);
        
        builder.Property(us => us.MatriarchGamesPlayed)
            .HasDefaultValue(0);
        
        builder.Property(us => us.MatriarchTriggered)
            .HasDefaultValue(0);
        
        builder.Property(us => us.MatriarchWins)
            .HasDefaultValue(0);
        
        builder.Property(us => us.WinsPerDeckJson)
            .HasColumnType("TEXT");
        
        builder.Property(us => us.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(us => new { us.UserId, us.SeasonId })
            .IsUnique();
    }
}
