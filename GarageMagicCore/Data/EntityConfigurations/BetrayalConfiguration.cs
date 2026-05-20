using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class BetrayalConfiguration : IEntityTypeConfiguration<Betrayal>
{
    public void Configure(EntityTypeBuilder<Betrayal> builder)
    {
        builder.ToTable("Betrayals");
        
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.Description)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(b => b.BetrayalDate)
            .IsRequired();
        
        builder.Property(b => b.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(b => b.BetrayerUserId);
        builder.HasIndex(b => b.VictimUserId);
        builder.HasIndex(b => b.BetrayalDate);
    }
}

