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

    public async Task<List<VodMovie>> GetVodStreamsAsync(XtreamConnectionInfo info)
    {
        var response = await _http.GetFromJsonAsync<List<ApiVodStream>>(info.VodStreamsUrl);
        if (response is null) return [];

        return response.Select(s => new VodMovie
        {
            StreamId = s.StreamId,
            Name = s.Name,
            Poster = s.StreamIcon,
            Plot = s.Plot,
            Year = s.Year,
            Rating = s.Rating,
            Director = s.Director,
            Cast = s.Cast,
            CategoryId = s.CategoryId,
            Extension = s.ContainerExtension ?? "mp4",
        }).ToList();
    }

    public async Task<List<VodSeriesItem>> GetSeriesAsync(XtreamConnectionInfo info)
    {
        var response = await _http.GetFromJsonAsync<List<ApiSeries>>(info.SeriesUrl);
        if (response is null) return [];

        return response.Select(s => new VodSeriesItem
        {
            SeriesId = s.SeriesId,
            Name = s.Name,
            Cover = s.Cover,
            Plot = s.Plot,
            Year = s.Year,
            Rating = s.Rating,
            CategoryId = s.CategoryId,
        }).ToList();
    }

    public async Task<List<VodEpisode>> GetSeriesEpisodesAsync(XtreamConnectionInfo info, int seriesId)
    {
        var response = await _http.GetFromJsonAsync<ApiSeriesInfo>(info.SeriesInfoUrl(seriesId));
        if (response?.Episodes is null) return [];

        var episodes = new List<VodEpisode>();
        foreach (var (seasonKey, apiEpisodes) in response.Episodes)
        {
            if (!int.TryParse(seasonKey, out var seasonNum)) continue;
            foreach (var apiEp in apiEpisodes)
            {
                episodes.Add(new VodEpisode
                {
                    Id = int.TryParse(apiEp.Id, out var id) ? id : 0,
                    EpisodeNum = apiEp.EpisodeNum,
                    SeasonNum = seasonNum,
                    Title = apiEp.Title,
                    Poster = apiEp.Info?.MovieImage,
                    Plot = apiEp.Info?.Plot,
                    Duration = apiEp.Info?.DurationSecs,
                    Url = info.EpisodeUrl(
                        int.TryParse(apiEp.Id, out var eid) ? eid : 0,
                        apiEp.ContainerExtension ?? "mp4"),
                });
            }
        }
        return episodes.OrderBy(e => e.SeasonNum).ThenBy(e => e.EpisodeNum).ToList();
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
