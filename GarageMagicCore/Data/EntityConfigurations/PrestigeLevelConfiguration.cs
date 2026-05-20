using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class PrestigeLevelConfiguration : IEntityTypeConfiguration<PrestigeLevel>
{
    public void Configure(EntityTypeBuilder<PrestigeLevel> builder)
    {
        builder.ToTable("PrestigeLevels");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Level)
            .IsRequired();
        
        builder.Property(p => p.AchievedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(p => new { p.UserId, p.SeasonId, p.Level });
    }
}

