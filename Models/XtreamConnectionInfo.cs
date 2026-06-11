namespace Sabeltann.Models;

public class XtreamConnectionInfo
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string PlayerApiUrl => $"{ServerUrl.TrimEnd('/')}/player_api.php";
    public string LiveStreamsUrl => $"{PlayerApiUrl}?username={Username}&password={Password}&action=get_live_streams";
    public string LiveCategoriesUrl => $"{PlayerApiUrl}?username={Username}&password={Password}&action=get_live_categories";
}
