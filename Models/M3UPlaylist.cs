namespace Sabeltann.Models;

public class M3UPlaylist
{
    public List<Channel> Channels { get; set; } = [];
    public string? SourceUrl { get; set; }
    public string? SourceFile { get; set; }
}
