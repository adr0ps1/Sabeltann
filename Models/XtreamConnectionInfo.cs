namespace Sabeltann.Models;

public class XtreamConnectionInfo
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    private string U => Uri.EscapeDataString(Username);
    private string P => Uri.EscapeDataString(Password);

    public string PlayerApiUrl => $"{ServerUrl.TrimEnd('/')}/player_api.php";
    public string LiveStreamsUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_live_streams";
    public string LiveCategoriesUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_live_categories";
    public string VodStreamsUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_vod_streams";
    public string VodCategoriesUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_vod_categories";
    public string SeriesUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_series";
    public string SeriesCategoriesUrl => $"{PlayerApiUrl}?username={U}&password={P}&action=get_series_categories";
    public string VodInfoUrl(int id) => $"{PlayerApiUrl}?username={U}&password={P}&action=get_vod_info&vod_id={id}";
    public string SeriesInfoUrl(int id) => $"{PlayerApiUrl}?username={U}&password={P}&action=get_series_info&series_id={id}";
    public string StreamUrl(int id, string ext) => $"{ServerUrl.TrimEnd('/')}/movie/{U}/{P}/{id}.{ext}";
    public string EpisodeUrl(int id, string ext) => $"{ServerUrl.TrimEnd('/')}/series/{U}/{P}/{id}.{ext}";
}
