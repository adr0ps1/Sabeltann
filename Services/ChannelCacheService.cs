using System.Text.Json;
using Sabeltann.Models;

namespace Sabeltann.Services;

public class ChannelCacheService
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sabeltann", "cache");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    static ChannelCacheService()
    {
        try { Directory.CreateDirectory(CacheDir); }
        catch { }
    }

    public void SaveChannels(string sourceKey, List<Models.Channel> channels)
    {
        try
        {
            var data = new ChannelCacheData
            {
                SourceKey = sourceKey,
                CachedAt = DateTime.UtcNow,
                Channels = channels.Select(c => new CachedChannel
                {
                    Name = c.Name,
                    Url = c.Url,
                    Group = c.Group,
                    Logo = c.Logo,
                    TvgId = c.TvgId,
                    TvgName = c.TvgName,
                    Duration = c.Duration,
                    Type = c.Type.ToString(),
                }).ToList(),
            };

            var path = GetCachePath(sourceKey);
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOpts));
            LogService.Info($"Cached {channels.Count} channels", new { path });
        }
        catch (Exception ex)
        {
            LogService.Warn("Failed to save channel cache", new { error = ex.Message });
        }
    }

    public List<Models.Channel>? LoadChannels(string sourceKey)
    {
        try
        {
            var path = GetCachePath(sourceKey);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ChannelCacheData>(json);
            if (data?.Channels is null) return null;

            return data.Channels.Select(c => new Models.Channel
            {
                Name = c.Name,
                Url = c.Url,
                Group = c.Group,
                Logo = c.Logo,
                TvgId = c.TvgId,
                TvgName = c.TvgName,
                Duration = c.Duration,
                Type = Enum.TryParse<Models.ChannelType>(c.Type, out var t) ? t : Models.ChannelType.LiveTv,
            }).ToList();
        }
        catch (Exception ex)
        {
            LogService.Warn("Failed to load channel cache", new { error = ex.Message });
            return null;
        }
    }

    public void Clear()
    {
        try
        {
            foreach (var f in Directory.GetFiles(CacheDir, "*.json"))
                File.Delete(f);
        }
        catch { }
    }

    private static string GetCacheKey(string source) => "v2_" + Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(source)))[..16];

    private static string GetCachePath(string sourceKey)
        => Path.Combine(CacheDir, $"channels_{GetCacheKey(sourceKey)}.json");

    private class ChannelCacheData
    {
        public string SourceKey { get; set; } = "";
        public DateTime CachedAt { get; set; }
        public List<CachedChannel> Channels { get; set; } = [];
    }

    private class CachedChannel
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Group { get; set; }
        public string? Logo { get; set; }
        public string? TvgId { get; set; }
        public string? TvgName { get; set; }
        public int Duration { get; set; } = -1;
        public string Type { get; set; } = "LiveTv";
    }
}
