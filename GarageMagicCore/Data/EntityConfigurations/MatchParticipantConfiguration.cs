using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("MatchParticipants");
        
        builder.HasKey(mp => mp.Id);
        
        builder.Property(mp => mp.HiddenRole)
            .HasConversion<string>();
        
        builder.Property(mp => mp.FinalRole)
            .HasConversion<string>();
        
        builder.Property(mp => mp.CreatedAt)
            .IsRequired();
        
        // Composite index to prevent duplicate participants
        builder.HasIndex(mp => new { mp.MatchId, mp.UserId })
            .IsUnique();
        
        // Relationships
        builder.HasOne(mp => mp.User)
            .WithMany()
            .HasForeignKey(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(mp => mp.Deck)
            .WithMany()
            .HasForeignKey(mp => mp.DeckId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

