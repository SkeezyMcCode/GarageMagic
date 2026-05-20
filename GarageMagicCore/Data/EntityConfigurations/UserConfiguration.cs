using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GarageMagicCore.Models;

namespace GarageMagicCore.Data.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.CurrentPrestigeLevel)
            .HasDefaultValue(0);
        
        builder.Property(u => u.IsApproved)
            .HasDefaultValue(false);
        
        builder.Property(u => u.IsAdmin)
            .HasDefaultValue(false);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique();
        
        builder.HasIndex(u => u.Email)
            .IsUnique();
        
        // Relationships
        builder.HasMany(u => u.Decks)
            .WithOne(d => d.User)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.Stats)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.PrestigeLevels)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(u => u.BetrayalsAsBetrayer)
            .WithOne(b => b.BetrayerUser)
            .HasForeignKey(b => b.BetrayerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(u => u.BetrayalsAsVictim)
            .WithOne(b => b.VictimUser)
            .HasForeignKey(b => b.VictimUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

