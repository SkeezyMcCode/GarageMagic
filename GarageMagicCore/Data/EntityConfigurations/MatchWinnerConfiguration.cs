using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class MatchWinnerConfiguration : IEntityTypeConfiguration<MatchWinner>
{
    public void Configure(EntityTypeBuilder<MatchWinner> builder)
    {
        builder.ToTable("MatchWinners");
        
        builder.HasKey(mw => mw.Id);
        
        builder.Property(mw => mw.CreatedAt)
            .IsRequired();
        
        // Composite index to prevent duplicate winners
        builder.HasIndex(mw => new { mw.MatchId, mw.UserId })
            .IsUnique();
        
        // Relationships
        builder.HasOne(mw => mw.User)
            .WithMany(u => u.MatchesAsWinner)
            .HasForeignKey(mw => mw.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

