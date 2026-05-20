namespace GarageMagicCore.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public int Id { get; set; }
    public required string SettingKey { get; set; }
    public required string SettingValue { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Constant keys for app settings
/// </summary>
public static class SettingKeys
{
    public const string WinsPerPrestigeLevel = "WinsPerPrestigeLevel";
    public const string CurrentSeasonId = "CurrentSeasonId";
}

