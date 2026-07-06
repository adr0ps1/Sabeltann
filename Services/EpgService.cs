using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Sabeltann.Models;

namespace Sabeltann.Services;

/// <summary>
/// Downloads and parses an XMLTV guide (Xtream's <c>xmltv.php</c> covers every channel in one call)
/// into programmes grouped by channel id. Channel ids match <c>Channel.TvgId</c> so the timeline can
/// join a guide to the live channel list.
/// ponytail: XDocument loads the whole guide into memory — fine for typical multi-MB EPG; switch to a
/// streaming XmlReader if a provider ships a huge guide.
/// </summary>
public sealed class EpgService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sabeltann", "epgcache");
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(3);

    static EpgService()
    {
#if DEBUG
        SelfCheck(); // throws (not a modal assert) so a parser regression crashes fast, never hangs the UI
#endif
    }

    public async Task<Dictionary<string, List<Programme>>> FetchAsync(string xmltvUrl, CancellationToken ct = default)
    {
        var cacheKey = CacheKey(xmltvUrl);
        if (TryReadCache(cacheKey) is { } cached)
            return cached;

        try
        {
            await using var stream = await Http.GetStreamAsync(xmltvUrl, ct);
            var guide = Parse(stream);
            WriteCache(cacheKey, guide);
            return guide;
        }
        catch (Exception ex)
        {
            LogService.Warn("EPG fetch failed", new { error = ex.Message });
            return [];
        }
    }

    private static string CacheKey(string url)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..12].ToLowerInvariant();

    private static Dictionary<string, List<Programme>>? TryReadCache(string key)
    {
        try
        {
            var path = Path.Combine(CacheDir, $"{key}.json");
            if (!File.Exists(path) || DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > CacheTtl)
                return null;
            return JsonSerializer.Deserialize<Dictionary<string, List<Programme>>>(File.ReadAllText(path));
        }
        catch { return null; }
    }

    private static void WriteCache(string key, Dictionary<string, List<Programme>> guide)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(Path.Combine(CacheDir, $"{key}.json"), JsonSerializer.Serialize(guide));
        }
        catch (Exception ex)
        {
            LogService.Warn("EPG cache write failed", new { error = ex.Message });
        }
    }

    public static Dictionary<string, List<Programme>> Parse(Stream xml)
        => Group(XDocument.Load(xml));

    private static Dictionary<string, List<Programme>> Group(XDocument doc)
    {
        var map = new Dictionary<string, List<Programme>>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in doc.Descendants("programme"))
        {
            var channel = (string?)p.Attribute("channel");
            if (string.IsNullOrEmpty(channel)) continue;

            var start = ParseXmltvTime((string?)p.Attribute("start"));
            var stop = ParseXmltvTime((string?)p.Attribute("stop"));
            if (start is null || stop is null || stop <= start) continue;

            var title = (string?)p.Element("title") ?? "(no title)";
            var desc = (string?)p.Element("desc");

            if (!map.TryGetValue(channel, out var list))
                map[channel] = list = [];
            list.Add(new Programme(channel, start.Value, stop.Value, title, desc));
        }
        // Stable order by start so equal-start programmes keep document order.
        foreach (var key in map.Keys.ToList())
            map[key] = map[key].OrderBy(p => p.StartUtc).ToList();
        return map;
    }

    /// <summary>Parse an XMLTV timestamp ("20260705180000 +0100") to UTC; null if malformed.</summary>
    internal static DateTime? ParseXmltvTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (s.Length < 14) return null;
        if (!DateTime.TryParseExact(s[..14], "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return null;

        var offset = TimeSpan.Zero;
        var rest = s.Length > 14 ? s[14..].Trim() : "";
        if (rest.Length >= 5 && (rest[0] == '+' || rest[0] == '-') &&
            int.TryParse(rest.AsSpan(1, 2), out var oh) && int.TryParse(rest.AsSpan(3, 2), out var om))
            offset = new TimeSpan(oh, om, 0) * (rest[0] == '-' ? -1 : 1);

        return new DateTimeOffset(dt, offset).UtcDateTime;
    }

    private static void SelfCheck()
    {
        const string sample = """
            <tv>
              <programme start="20260705180000 +0100" stop="20260705190000 +0100" channel="a">
                <title>News</title><desc>Headlines</desc>
              </programme>
              <programme start="20260705160000 +0000" stop="20260705163000 +0000" channel="a">
                <title>Weather</title>
              </programme>
              <programme start="bad" stop="also-bad" channel="a"><title>Skip me</title></programme>
            </tv>
            """;
        var map = Parse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sample)));
        Check(map.Count == 1, "expected one channel");
        Check(map["a"].Count == 2, "expected two valid programmes (malformed one skipped)");
        Check(map["a"][0].Title == "Weather", "expected sort by start: 16:00Z before 18:00+01:00=17:00Z");
        Check(ParseXmltvTime("20260705180000 +0100") == new DateTime(2026, 7, 5, 17, 0, 0, DateTimeKind.Utc),
            "expected +0100 offset applied to reach UTC");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"EpgService self-check failed: {message}");
    }
}
