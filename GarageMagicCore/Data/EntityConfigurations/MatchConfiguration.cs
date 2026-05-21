using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.MatchType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(m => m.MatchDate)
            .IsRequired();
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(m => m.MatchDate);
        builder.HasIndex(m => m.MatchType);
        
        // Relationships
        builder.HasOne(m => m.Deck)
            .WithMany(d => d.Matches)
            .HasForeignKey(m => m.DeckId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(m => m.SheriffUser)
            .WithMany()
            .HasForeignKey(m => m.SheriffUserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(m => m.MatriarchUser)
            .WithMany()
            .HasForeignKey(m => m.MatriarchUserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasMany(m => m.Winners)
            .WithOne(w => w.Match)
            .HasForeignKey(w => w.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(m => m.Participants)
            .WithOne(p => p.Match)
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

