namespace Sabeltann.Models;

public class Channel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? TvgId { get; set; }
    public string? TvgName { get; set; }
    public string? Logo { get; set; }
    public string? Group { get; set; }
    public int Duration { get; set; } = -1;
    public ChannelType Type { get; set; } = ChannelType.LiveTv;
}

public enum ChannelType
{
    LiveTv,
    Movie,
    Series
}
