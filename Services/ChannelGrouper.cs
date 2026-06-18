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

    public static string? ExtractShowName(string channelName)
    {
        var m = SeasonEpisodePattern.Match(channelName);
        if (m.Success) return m.Groups[1].Value.Trim();

        m = EpisodePattern.Match(channelName);
        if (m.Success) return m.Groups[1].Value.Trim();

        return null;
    }
}
