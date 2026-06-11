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
    public string? LastCategoryName { get; set; }
}

public class XtreamSettings
{
    public string ServerUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
