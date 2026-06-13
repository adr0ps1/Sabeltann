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
        "live", "tv", "news", "sport", "sports", "entertainment", "general",
        "music", "radio", "kids", "children", "documentary", "education",
        "local", "regional", "national", "international", "24/7", "hd",
        "fhd", "4k", "ultra", "premium", "vip", "exclusive", "adult",
        "religion", "shopping", "weather", "travel", "food", "lifestyle",
        "reality", "talk", "game", "comedy", "drama", "action", "thriller",
    ];

    private static readonly HashSet<string> VodGroups =
    [
        "movie", "movies", "film", "films", "cinema", "vod", "series",
        "show", "shows", "season", "seasons", "episode", "episodes",
        "anime", "cartoon", "animation", "docuseries", "miniseries",
        "tv series", "tv shows", "box set", "collection",
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
