using Sabeltann.Models;

namespace Sabeltann.Services;

public class M3UParser
{
    public M3UPlaylist Parse(string content)
    {
        var playlist = new M3UPlaylist();
        var lines = content.Split('\n');

        Channel? current = null;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim().Trim('\r');
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                current = ParseExtInf(line);
            }
            else if (!line.StartsWith("#") && current != null)
            {
                current.Url = NormalizeUrl(line);
                if (string.IsNullOrEmpty(current.Name))
                    current.Name = MakeNameFromUrl(current.Url);
                if (!string.IsNullOrEmpty(current.Url))
                    playlist.Channels.Add(current);
                current = null;
            }
        }

        return playlist;
    }

    private static string NormalizeUrl(string url)
    {
        var u = url.Trim().Replace('\\', '/');
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
        {
            u = "http://" + u;
        }
        return u;
    }

    private static string MakeNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var last = uri.Segments.LastOrDefault()?.Trim('/') ?? url;
            return Uri.UnescapeDataString(last);
        }
        catch
        {
            var parts = url.Split('/', '\\');
            return parts.Length > 0 ? parts[^1] : url;
        }
    }

    private static Channel ParseExtInf(string line)
    {
        var ch = new Channel();

        var commaIdx = FindNameSeparator(line);
        if (commaIdx < 0) return ch;

        var metaPart = line[8..commaIdx];
        var namePart = line[(commaIdx + 1)..].Trim();

        ch.Name = namePart;

        var parts = metaPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out var dur))
            ch.Duration = dur;

        ch.TvgId = ExtractAttr(metaPart, "tvg-id");
        ch.TvgName = ExtractAttr(metaPart, "tvg-name");
        ch.Logo = ExtractAttr(metaPart, "tvg-logo");
        ch.Group = ExtractAttr(metaPart, "group-title");

        return ch;
    }

    /// <summary>
    /// Finds the first comma that is NOT inside a double-quoted string.
    /// This is the separator between the EXTINF metadata and the channel name.
    /// </summary>
    private static int FindNameSeparator(string line)
    {
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) return i;
        }
        return -1;
    }

    private static string? ExtractAttr(string input, string attr)
    {
        var marker = $"{attr}=\"";
        var start = input.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        start += marker.Length;
        var end = input.IndexOf('"', start);
        return end < 0 ? null : input[start..end];
    }

    public async Task<M3UPlaylist> LoadFromUrlAsync(string url)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Sabeltann/1.0");
        var content = await http.GetStringAsync(url);
        var playlist = Parse(content);
        playlist.SourceUrl = url;
        return playlist;
    }

    public Task<M3UPlaylist> LoadFromFileAsync(string path)
    {
        var content = File.ReadAllText(path);
        var playlist = Parse(content);
        playlist.SourceFile = path;
        return Task.FromResult(playlist);
    }
}
