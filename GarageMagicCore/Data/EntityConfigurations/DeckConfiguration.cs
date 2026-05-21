using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class DeckConfiguration : IEntityTypeConfiguration<Deck>
{
    public void Configure(EntityTypeBuilder<Deck> builder)
    {
        builder.ToTable("Decks");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.DeckName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(d => d.CommanderName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(d => d.ColorIdentity)
            .HasMaxLength(10);

        builder.Property(d => d.CommanderImageUri)
            .HasMaxLength(500);

        builder.Property(d => d.ScryfallId)
            .HasMaxLength(40);
        
        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);
        
        builder.Property(d => d.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(d => new { d.UserId, d.DeckName });
        
        // Relationships configured in UserConfiguration
    }
}

