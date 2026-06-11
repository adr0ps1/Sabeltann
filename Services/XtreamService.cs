using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Sabeltann.Models;

namespace Sabeltann.Services;

public class XtreamService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<XtreamConnectionInfo> ValidateAsync(XtreamConnectionInfo info)
    {
        var url = $"{info.PlayerApiUrl}?username={info.Username}&password={info.Password}";
        var response = await _http.GetFromJsonAsync<XtreamUserInfo>(url);
        if (response?.User?.Username is null)
            throw new InvalidOperationException("Xtream authentication failed");

        return info;
    }

    public async Task<List<Channel>> GetLiveStreamsAsync(XtreamConnectionInfo info)
    {
        var response = await _http.GetFromJsonAsync<List<XtreamStream>>(info.LiveStreamsUrl);
        if (response is null) return [];

        return response.Select(s => new Channel
        {
            Name = s.Name,
            Url = $"{info.ServerUrl.TrimEnd('/')}/live/{info.Username}/{info.Password}/{s.StreamId}.m3u8",
            Logo = s.StreamIcon,
            Group = s.CategoryId.ToString(),
            TvgId = s.EpgChannelId,
            TvgName = s.Name,
        }).ToList();
    }

    private record XtreamUserInfo(XtreamUser? User);
    private record XtreamUser(string? Username);
    private record XtreamStream(
        [property: JsonPropertyName("stream_id")] int StreamId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("stream_icon")] string? StreamIcon,
        [property: JsonPropertyName("category_id")] string CategoryId,
        [property: JsonPropertyName("epg_channel_id")] string? EpgChannelId);
}
