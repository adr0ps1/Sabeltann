using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Sabeltann.Models;

namespace Sabeltann.Services;

public class XtreamService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task<XtreamConnectionInfo> ValidateAsync(XtreamConnectionInfo info)
    {
        var url = $"{info.PlayerApiUrl}?username={Uri.EscapeDataString(info.Username)}&password={Uri.EscapeDataString(info.Password)}";
        var response = await Http.GetFromJsonAsync<XtreamUserInfo>(url);
        if (response?.User?.Username is null)
            throw new InvalidOperationException("Xtream authentication failed");

        return info;
    }

    public async Task<List<Channel>> GetLiveStreamsAsync(XtreamConnectionInfo info)
    {
        var categoriesTask = GetLiveCategoriesAsync(info);
        var responseTask = Http.GetFromJsonAsync<List<XtreamStream>>(info.LiveStreamsUrl);

        await Task.WhenAll(categoriesTask, responseTask);

        var response = responseTask.Result;
        if (response is null) return [];

        var categoryNames = categoriesTask.Result;

        return response.Select(s => new Channel
        {
            Name = s.Name,
            Url = $"{info.ServerUrl.TrimEnd('/')}/live/{Uri.EscapeDataString(info.Username)}/{Uri.EscapeDataString(info.Password)}/{s.StreamId}.m3u8",
            Logo = s.StreamIcon,
            Group = categoryNames.TryGetValue(s.CategoryId, out var catName) ? catName : s.CategoryId,
            TvgId = s.EpgChannelId,
            TvgName = s.Name,
            Type = ChannelType.LiveTv,
        }).ToList();
    }

    public async Task<Dictionary<string, string>> GetLiveCategoriesAsync(XtreamConnectionInfo info)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<List<XtreamCategory>>(info.LiveCategoriesUrl);
            if (response is null) return [];
            return response
                .Where(c => c.CategoryId is not null && c.CategoryName is not null)
                .ToDictionary(c => c.CategoryId!, c => c.CategoryName!, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            LogService.Warn("Failed to load Xtream live categories", new { error = ex.Message });
            return [];
        }
    }

    public async Task<List<VodMovie>> GetVodStreamsAsync(XtreamConnectionInfo info)
    {
        var response = await Http.GetFromJsonAsync<List<ApiVodStream>>(info.VodStreamsUrl);
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
        var response = await Http.GetFromJsonAsync<List<ApiSeries>>(info.SeriesUrl);
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
        var response = await Http.GetFromJsonAsync<ApiSeriesInfo>(info.SeriesInfoUrl(seriesId));
        if (response?.Episodes is null) return [];

        var episodes = new List<VodEpisode>();
        foreach (var (seasonKey, apiEpisodes) in response.Episodes)
        {
            if (!int.TryParse(seasonKey, out var seasonNum)) continue;
            foreach (var apiEp in apiEpisodes)
            {
                if (!int.TryParse(apiEp.Id, out var eid)) eid = 0;
                episodes.Add(new VodEpisode
                {
                    Id = eid,
                    EpisodeNum = apiEp.EpisodeNum,
                    SeasonNum = seasonNum,
                    Title = apiEp.Title,
                    Poster = apiEp.Info?.MovieImage,
                    Plot = apiEp.Info?.Plot,
                    Duration = apiEp.Info?.DurationSecs,
                    Url = info.EpisodeUrl(eid, apiEp.ContainerExtension ?? "mp4"),
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

    private record XtreamCategory(
        [property: JsonPropertyName("category_id")] string? CategoryId,
        [property: JsonPropertyName("category_name")] string? CategoryName);
}
