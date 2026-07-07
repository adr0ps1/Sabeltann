using System.Text.Json;

namespace Sabeltann.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Sabeltann", "settings.json");

    public SettingsData Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
        }
        catch { }
        return new SettingsData();
    }

    public void Save(SettingsData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}

public class SettingsData
{
    public string? LastSourceType { get; set; }
    public string? LastSourceUrl { get; set; }
    public string? LastSourceFile { get; set; }
    public XtreamSettings? LastXtream { get; set; }
    public List<string> FavoriteUrls { get; set; } = [];
    public string? LastChannelUrl { get; set; }
    /// <summary>Last-opened section (live/movies/series) so login lands there directly.</summary>
    public string? LastSection { get; set; }
    public string? LastCategoryName { get; set; }
    public int DefaultVolume { get; set; } = 100;
    public bool AutoLoadLastSession { get; set; } = true;
    public string? LastServerUrl { get; set; }
    public string? LastUsername { get; set; }
    public string? OmdbApiKey { get; set; }
    public bool CheckForUpdatesEnabled { get; set; } = true;
    public bool IncludePrerelease { get; set; } = false;
    public DateTime? LastUpdateCheck { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
    public Dictionary<string, VodProgressEntry> VodProgress { get; set; } = [];
}

public class VodProgressEntry
{
    public long PositionMs { get; set; }
    public long DurationMs { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class XtreamSettings
{
    public string ServerUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
