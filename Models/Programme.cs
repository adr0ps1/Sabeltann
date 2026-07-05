namespace Sabeltann.Models;

/// <summary>One EPG programme, keyed to a channel by its XMLTV id (matches <c>Channel.TvgId</c>).</summary>
public record Programme(string ChannelId, DateTime StartUtc, DateTime StopUtc, string Title, string? Description)
{
    public bool IsAiringAt(DateTime utc) => StartUtc <= utc && utc < StopUtc;
}
