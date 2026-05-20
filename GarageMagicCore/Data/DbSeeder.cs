using GarageMagicCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GarageMagicCore.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(GarageMagicDbContext context, IConfiguration config)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();
        
        // Seed default app settings if they don't exist
        if (!await context.AppSettings.AnyAsync())
        {
            await SeedAppSettingsAsync(context);
        }
        
        // Create initial season if none exist
        if (!await context.Seasons.AnyAsync())
        {
            await SeedInitialSeasonAsync(context);
        }

        // Seed admin users from config
        await SeedAdminsAsync(context, config);
        
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedAdminsAsync(GarageMagicDbContext context, IConfiguration config)
    {
        var adminConfigs = config.GetSection("SeedAdmins").GetChildren().ToList();
        foreach (var adminConfig in adminConfigs)
        {
            var username = adminConfig["Username"];
            var email = adminConfig["Email"];
            var password = adminConfig["Password"];

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                continue;

            if (await context.Users.AnyAsync(u => u.Username == username))
                continue; // Already exists

            context.Users.Add(new User
            {
                Username = username,
                Email = email ?? $"{username}@garagemagic.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsApproved = true,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedAppSettingsAsync(GarageMagicDbContext context)
    {
        var settings = new[]
        {
            new AppSettings
            {
                SettingKey = SettingKeys.WinsPerPrestigeLevel,
                SettingValue = "5",
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        await context.AppSettings.AddRangeAsync(settings);
    }
    
    private static async Task SeedInitialSeasonAsync(GarageMagicDbContext context)
    {
        var now = DateTime.UtcNow;
        var currentQuarter = GetCurrentQuarter(now);
        
        var season = new Season
        {
            Name = $"{now.Year} {currentQuarter}",
            Year = now.Year,
            Quarter = currentQuarter,
            StartDate = GetQuarterStartDate(now.Year, currentQuarter),
            EndDate = GetQuarterEndDate(now.Year, currentQuarter),
            IsActive = true,
            CreatedAt = now
        };
        
        await context.Seasons.AddAsync(season);
        await context.SaveChangesAsync();
        
        // Set current season in app settings
        await context.AppSettings.AddAsync(new AppSettings
        {
            SettingKey = SettingKeys.CurrentSeasonId,
            SettingValue = season.Id.ToString(),
            UpdatedAt = now
        });
    }
    
    private static Quarter GetCurrentQuarter(DateTime date)
    {
        return date.Month switch
        {
            >= 1 and <= 3 => Quarter.Q1,
            >= 4 and <= 6 => Quarter.Q2,
            >= 7 and <= 9 => Quarter.Q3,
            _ => Quarter.Q4
        };
    }
    
    private static DateTime GetQuarterStartDate(int year, Quarter quarter)
    {
        return quarter switch
        {
            Quarter.Q1 => new DateTime(year, 1, 1),
            Quarter.Q2 => new DateTime(year, 4, 1),
            Quarter.Q3 => new DateTime(year, 7, 1),
            Quarter.Q4 => new DateTime(year, 10, 1),
            _ => throw new ArgumentException("Invalid quarter")
        };
    }
    
    private static DateTime GetQuarterEndDate(int year, Quarter quarter)
    {
        return quarter switch
        {
            Quarter.Q1 => new DateTime(year, 3, 31, 23, 59, 59),
            Quarter.Q2 => new DateTime(year, 6, 30, 23, 59, 59),
            Quarter.Q3 => new DateTime(year, 9, 30, 23, 59, 59),
            Quarter.Q4 => new DateTime(year, 12, 31, 23, 59, 59),
            _ => throw new ArgumentException("Invalid quarter")
        };
    }
}

