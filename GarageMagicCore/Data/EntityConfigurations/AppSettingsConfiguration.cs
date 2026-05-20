using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.ToTable("AppSettings");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.SettingKey)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(a => a.SettingValue)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(a => a.UpdatedAt)
            .IsRequired();
        
        // Unique constraint on SettingKey
        builder.HasIndex(a => a.SettingKey)
            .IsUnique();
    }
}

