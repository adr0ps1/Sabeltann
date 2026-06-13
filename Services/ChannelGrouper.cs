using System.Text.RegularExpressions;
using Sabeltann.ViewModels;

namespace Sabeltann.Services;

public static class ChannelGrouper
{
    private static readonly Regex EpisodePattern = new(
        @"^(.+?)\s*[-–:]\s*(?:Episode|Ep|Part)\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SeasonEpisodePattern = new(
        @"^(.+?)\s+S\d{1,2}E\d{1,2}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> LiveGroups =
    [
        "norway", "sweden", "denmark", "finland", "ukraine",
        "norwegian", "swedish", "danish", "finnish",
        "uk ", "canada", "germany", "france", "spain", "italy",
        "sport", "football", "premier league", "ufc", "mma", "boxing", "wwe",
        "fifa", "world cup", "olympics", "racing", "motorsport",
        "news", "radio", "24/7",
        "4k", "uhd", "fhd", "hd",
        "live events", "ppv",
        "multi audio", "multi subs",
        "local", "regional",
    ];

    private static readonly HashSet<string> VodGroups =
    [
        "movie", "movies", "film", "cinema", "vod",
        "series", "shows", "season", "episode",
        "action", "adventure", "animation", "anime", "comedy", "crime",
        "documentar", "drama", "family", "fantasy", "horror", "kids",
        "martial arts", "music", "musical", "concert", "mystery", "thriller",
        "reality", "romance", "scifi", "science fiction", "war", "western",
        "classics", "disney", "x-mas", "christmas",
        "scandinavian reality", "scandinavian series",
        "adult", "xxx", "2026", "2025", "2024",
    ];

    public static string? ExtractShowName(string channelName)
    {
        var m = SeasonEpisodePattern.Match(channelName);
        if (m.Success) return m.Groups[1].Value.Trim();

        m = EpisodePattern.Match(channelName);
        if (m.Success) return m.Groups[1].Value.Trim();

        return null;
    }

    public static bool IsVodContent(string channelName, string? groupName)
    {
        if (groupName is not null)
        {
            var g = groupName.ToLowerInvariant().Trim();
            if (VodGroups.Any(v => g.Contains(v))) return true;
            if (LiveGroups.Any(l => g.Contains(l))) return false;
        }

        var name = channelName.ToLowerInvariant();
        if (VodGroups.Any(v => name.Contains(v))) return true;
        if (LiveGroups.Any(l => name.Contains(l))) return false;

        return ExtractShowName(channelName) is not null;
    }

    public static (List<ChannelListItemViewModel> Live, List<ChannelListItemViewModel> Vod)
        SplitByType(IEnumerable<ChannelListItemViewModel> channels)
    {
        var live = new List<ChannelListItemViewModel>();
        var vod = new List<ChannelListItemViewModel>();
        foreach (var ch in channels)
        {
            if (IsVodContent(ch.Name, ch.Group))
                vod.Add(ch);
            else
                live.Add(ch);
        }
        return (live, vod);
    }

    public static List<ShowGroup> GroupChannels(IEnumerable<ChannelListItemViewModel> channels)
    {
        var groups = new Dictionary<string, ShowGroup>();

        foreach (var ch in channels)
        {
            var showName = ExtractShowName(ch.Name);
            if (showName is not null)
            {
                if (!groups.TryGetValue(showName, out var group))
                {
                    group = new ShowGroup { Name = showName };
                    groups[showName] = group;
                }
                group.Channels.Add(ch);
            }
        }

        return groups.Values.OrderBy(g => g.Name).ToList();
    }
}

public class ShowGroup
{
    public string Name { get; set; } = "";
    public List<ChannelListItemViewModel> Channels { get; set; } = [];
    public int Count => Channels.Count;
}
