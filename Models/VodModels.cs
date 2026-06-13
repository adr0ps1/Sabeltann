using System.Text.Json.Serialization;

namespace Sabeltann.Models;

public class VodMovie
{
    public int StreamId { get; set; }
    public string Name { get; set; } = "";
    public string? Poster { get; set; }
    public string? Plot { get; set; }
    public string? Year { get; set; }
    public string? Rating { get; set; }
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public string? CategoryId { get; set; }
    public string? Extension { get; set; }
}

public class VodSeriesItem
{
    public int SeriesId { get; set; }
    public string Name { get; set; } = "";
    public string? Cover { get; set; }
    public string? Plot { get; set; }
    public string? Year { get; set; }
    public string? Rating { get; set; }
    public string? CategoryId { get; set; }
    public int SeasonCount { get; set; }
}

public class VodEpisode
{
    public int Id { get; set; }
    public int EpisodeNum { get; set; }
    public int SeasonNum { get; set; }
    public string Title { get; set; } = "";
    public string? Poster { get; set; }
    public string? Plot { get; set; }
    public string? Duration { get; set; }
    public string? Url { get; set; }
}

// API response types
public record ApiVodStream(
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stream_icon")] string? StreamIcon,
    [property: JsonPropertyName("rating")] string? Rating,
    [property: JsonPropertyName("category_id")] string? CategoryId,
    [property: JsonPropertyName("container_extension")] string? ContainerExtension,
    [property: JsonPropertyName("year")] string? Year,
    [property: JsonPropertyName("director")] string? Director,
    [property: JsonPropertyName("cast")] string? Cast,
    [property: JsonPropertyName("plot")] string? Plot);

public record ApiSeries(
    [property: JsonPropertyName("series_id")] int SeriesId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("cover")] string? Cover,
    [property: JsonPropertyName("plot")] string? Plot,
    [property: JsonPropertyName("year")] string? Year,
    [property: JsonPropertyName("rating")] string? Rating,
    [property: JsonPropertyName("category_id")] string? CategoryId);

public record ApiSeriesInfo(
    [property: JsonPropertyName("episodes")] Dictionary<string, List<ApiEpisode>>? Episodes);

public record ApiEpisode(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("episode_num")] int EpisodeNum,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("container_extension")] string? ContainerExtension,
    [property: JsonPropertyName("info")] ApiEpisodeInfo? Info);

public record ApiEpisodeInfo(
    [property: JsonPropertyName("movie_image")] string? MovieImage,
    [property: JsonPropertyName("plot")] string? Plot,
    [property: JsonPropertyName("duration_secs")] string? DurationSecs);
