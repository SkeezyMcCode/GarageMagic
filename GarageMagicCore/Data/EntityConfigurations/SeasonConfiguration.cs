using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.Year)
            .IsRequired();
        
        builder.Property(s => s.Quarter)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(s => s.StartDate)
            .IsRequired();
        
        builder.Property(s => s.EndDate)
            .IsRequired();
        
        builder.Property(s => s.IsActive)
            .HasDefaultValue(false);
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(s => new { s.Year, s.Quarter })
            .IsUnique();
        
        builder.HasIndex(s => s.IsActive);
        
        // Relationships
        builder.HasMany(s => s.UserStats)
            .WithOne(us => us.Season)
            .HasForeignKey(us => us.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(s => s.PrestigeLevels)
            .WithOne(p => p.Season)
            .HasForeignKey(p => p.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

