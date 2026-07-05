using System.Diagnostics;
using System.Globalization;
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

    static EpgService()
    {
#if DEBUG
        SelfCheck();
#endif
    }

    public async Task<Dictionary<string, List<Programme>>> FetchAsync(string xmltvUrl, CancellationToken ct = default)
    {
        try
        {
            await using var stream = await Http.GetStreamAsync(xmltvUrl, ct);
            return Parse(stream);
        }
        catch (Exception ex)
        {
            LogService.Warn("EPG fetch failed", new { error = ex.Message });
            return [];
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
        foreach (var list in map.Values)
            list.Sort((a, b) => a.StartUtc.CompareTo(b.StartUtc));
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
              <programme start="20260705170000 +0000" stop="20260705173000 +0000" channel="a">
                <title>Weather</title>
              </programme>
              <programme start="bad" stop="also-bad" channel="a"><title>Skip me</title></programme>
            </tv>
            """;
        var map = Parse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sample)));
        Debug.Assert(map.Count == 1, "one channel");
        Debug.Assert(map["a"].Count == 2, "two valid programmes, malformed one skipped");
        Debug.Assert(map["a"][0].Title == "Weather", "sorted by start (17:00 UTC before 17:00+01:00=18:00)");
        Debug.Assert(ParseXmltvTime("20260705180000 +0100") == new DateTime(2026, 7, 5, 17, 0, 0, DateTimeKind.Utc),
            "offset applied to reach UTC");
    }
}
