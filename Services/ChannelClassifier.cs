using System.Text.RegularExpressions;
using Sabeltann.Models;

namespace Sabeltann.Services;

public static class ChannelClassifier
{
    // S1E1 through S9999E9999 — supports multi-digit seasons like S2025E113
    private static readonly Regex SeasonEpisode = new(
        @"\bS(\d{1,4})\s*E(\d{1,4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EpisodeNum = new(
        @"\b(?:Episode|Ep|Part)\s*\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AltEpisode = new(
        @"\b(\d{1,2})x(\d{1,3})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // "Season 1", "Season 12", "Season.1", "Season_1"
    private static readonly Regex SeasonKeyword = new(
        @"\bSeason\s*\.?_?\s*\d", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Standalone 4-digit year (1900-2099) with word boundaries on both sides
    private static readonly Regex HasYear = new(
        @"(?<!\d)(19|20)\d{2}(?!\d)");

    public static ChannelType Classify(Channel channel)
    {
        var name = channel.Name ?? "";
        var tvgName = channel.TvgName ?? "";
        var checkName = string.IsNullOrWhiteSpace(name) ? tvgName : name;
        var group = (channel.Group ?? "").ToLowerInvariant().Trim();
        var url = channel.Url ?? "";

        // 1. Strongest signals first: season/episode or year in the name.
        //    These override transport/EPG hints because providers often leave
        //    a stale tvg-id or serve VOD over HLS (.m3u8).
        if (SeasonEpisode.IsMatch(checkName) || AltEpisode.IsMatch(checkName))
            return ChannelType.Series;

        if (EpisodeNum.IsMatch(checkName))
            return ChannelType.Series;

        if (SeasonKeyword.IsMatch(checkName))
            return ChannelType.Series;

        if (HasYear.IsMatch(checkName))
            return ChannelType.Movie;

        // 2. VOD file extension = movie (unless group indicates series).
        bool isVod = url.Contains(".mkv", StringComparison.OrdinalIgnoreCase) ||
                     url.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ||
                     url.Contains(".avi", StringComparison.OrdinalIgnoreCase);

        if (isVod)
        {
            if (IsSeriesGroup(group))
                return ChannelType.Series;

            return ChannelType.Movie;
        }

        // 3. EPG tvg-id = broadcast channel (only when no VOD signal was found).
        if (!string.IsNullOrWhiteSpace(channel.TvgId))
            return ChannelType.LiveTv;

        // 4. HLS stream (.m3u8 or .ts) = broadcast (VOD HLS was caught by name/extension above).
        if (url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/m3u8", StringComparison.OrdinalIgnoreCase) ||
            url.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            return ChannelType.LiveTv;

        // 5. Group heuristics for unknown URL types.
        if (IsLiveGroup(group))
            return ChannelType.LiveTv;
        if (IsSeriesGroup(group))
            return ChannelType.Series;

        return ChannelType.LiveTv;
    }

    private static bool IsSeriesGroup(string group)
    {
        if (string.IsNullOrEmpty(group)) return false;
        if (DefinitelySeries.Contains(group)) return true;
        // "contains" check for broader matching
        return group.Contains("series") ||
               group.Contains("reality") ||
               group.Contains("tv show");
    }

    private static bool IsLiveGroup(string group)
    {
        if (string.IsNullOrEmpty(group)) return false;
        if (DefinitelyLive.Contains(group)) return true;
        return group.Contains("sport") ||
               group.Contains("news") ||
               group.Contains("radio");
    }

    private static readonly HashSet<string> DefinitelyLive = new(StringComparer.OrdinalIgnoreCase)
    {
        "norway", "sweden", "denmark", "finland", "iceland", "ukraine",
        "uk", "canada", "canada/us", "germany", "france", "spain", "italy",
        "all sport", "premier league", "ufc/mma/boxing/wwe/sport",
        "live events - ppv", "4k/uhd", "multi audio",
    };

    private static readonly HashSet<string> DefinitelySeries = new(StringComparer.OrdinalIgnoreCase)
    {
        "scandinavian series", "scandinavian reality & tv shows",
    };
}
