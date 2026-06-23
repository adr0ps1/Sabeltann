using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public partial class SeriesShowViewModel : ObservableObject
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public string? Year { get; set; }
    public int? SeriesId { get; set; }
    public string? Cover { get; set; }
    public List<ChannelListItemViewModel> Episodes { get; set; } = [];

    [ObservableProperty]
    private Bitmap? _coverSrc;

    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";

    public string Subtitle => Year is not null
        ? Year
        : Count > 0 ? $"{Count} episodes" : "";

    public void BeginLoadCover()
    {
        if (Cover is not null)
            _ = LoadCoverAsync();
    }

    private async Task LoadCoverAsync()
    {
        var bmp = await ImageService.LoadAsync(Cover);
        if (bmp is not null)
            CoverSrc = bmp;
    }
}

public partial class SeriesBrowserViewModel : ObservableObject
{
    private readonly XtreamService _xtream = new();
    private XtreamConnectionInfo? _xtreamInfo;

    private List<ChannelListItemViewModel> _allM3uEpisodes = [];

    public event Action<string>? PlayRequested;

    [ObservableProperty]
    private ObservableCollection<SeriesShowViewModel> _shows = [];

    [ObservableProperty]
    private ObservableCollection<SeasonGroup> _seasonGroups = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [];

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowShows))]
    [NotifyPropertyChangedFor(nameof(ShowEpisodes))]
    private bool _isShowingEpisodes;

    public bool ShowShows => !IsShowingEpisodes;
    public bool ShowEpisodes => IsShowingEpisodes;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _currentShowName = "";

    [ObservableProperty]
    private bool _isLoading;

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (!IsShowingEpisodes)
            RebuildShows();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!IsShowingEpisodes)
            RebuildShows();
    }

    public void InitializeFromEpisodes(List<ChannelListItemViewModel> episodes)
    {
        _xtreamInfo = null;
        _allM3uEpisodes = episodes;
        IsLoading = true;
        StatusText = "Grouping series...";
        IsShowingEpisodes = false;

        BuildCategories(episodes);
        RebuildShows();
        IsLoading = false;
    }

    private void BuildCategories(List<ChannelListItemViewModel> episodes)
    {
        Categories.Clear();
        var groups = episodes
            .Select(ch => ch.Group ?? "Uncategorized")
            .Distinct()
            .OrderBy(g => g == "Uncategorized" ? 1 : 0)
            .ThenBy(g => g)
            .ToList();
        Categories.Add("All");
        foreach (var g in groups)
            Categories.Add(g);
        SelectedCategory = "All";
    }

    private void RebuildShows()
    {
        if (_xtreamInfo is not null) return;

        var cat = SelectedCategory ?? "All";
        var query = (SearchText ?? "").Trim().ToLowerInvariant();

        IEnumerable<ChannelListItemViewModel> pool = _allM3uEpisodes
            .Where(ch => !IsGarbageEntry(ch.Name));
        if (cat != "All")
            pool = pool.Where(ch => (ch.Group ?? "Uncategorized").Equals(cat, StringComparison.OrdinalIgnoreCase));

        const int maxEpisodes = 2000;
        var sample = pool.Take(maxEpisodes).ToList();

        var grouped = sample
            .Select(ch => new { Channel = ch, ShowName = ChannelGrouper.ExtractShowName(ch.Name) })
            .Where(x => x.ShowName is not null);

        if (query.Length > 0)
            grouped = grouped.Where(x => x.ShowName!.Contains(query, StringComparison.OrdinalIgnoreCase));

        var shows = grouped
            .GroupBy(x => x.ShowName!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Take(500)
            .ToList();

        Shows.Clear();
        foreach (var g in shows)
        {
            var episodes = g.Select(x => x.Channel).ToList();
            var show = new SeriesShowViewModel
            {
                Name = g.Key,
                Count = g.Count(),
                Episodes = episodes,
                Cover = episodes.FirstOrDefault()?.Logo
            };
            show.BeginLoadCover();
            Shows.Add(show);
        }

        var totalEpisodes = pool.Count();
        StatusText = totalEpisodes > maxEpisodes
            ? $"{Shows.Count} shows ({maxEpisodes} of {totalEpisodes} episodes processed)"
            : $"{Shows.Count} shows, {totalEpisodes} episodes";
    }

    public async Task InitializeFromXtreamAsync(XtreamConnectionInfo info)
    {
        _xtreamInfo = info;
        IsLoading = true;
        StatusText = "Loading series...";
        IsShowingEpisodes = false;
        Shows.Clear();

        try
        {
            var series = await _xtream.GetSeriesAsync(info);
            foreach (var s in series)
            {
                var show = new SeriesShowViewModel
                {
                    Name = s.Name,
                    Year = s.Year,
                    SeriesId = s.SeriesId,
                    Cover = s.Cover
                };
                show.BeginLoadCover();
                Shows.Add(show);
            }
            StatusText = $"{Shows.Count} series loaded";
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load Xtream series", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SelectShow(SeriesShowViewModel? show)
    {
        if (show is null) return;
        IsShowingEpisodes = true;
        CurrentShowName = show.Name;

        SeasonGroups.Clear();

        if (_xtreamInfo is not null && show.SeriesId is int seriesId)
        {
            await LoadXtreamEpisodesAsync(_xtreamInfo, seriesId);
        }
        else
        {
            BuildM3uSeasonGroups(show.Episodes);
        }
    }

    private async Task LoadXtreamEpisodesAsync(XtreamConnectionInfo info, int seriesId)
    {
        IsLoading = true;
        StatusText = $"Loading episodes...";
        try
        {
            var episodes = await _xtream.GetSeriesEpisodesAsync(info, seriesId);
            var groups = episodes
                .GroupBy(e => e.SeasonNum)
                .OrderBy(g => g.Key);

            foreach (var g in groups)
            {
                SeasonGroups.Add(new SeasonGroup
                {
                    SeasonNumber = g.Key,
                    Episodes = g.OrderBy(e => e.EpisodeNum)
                        .Select(e => new VodEpisodeViewModel(e, e.Poster))
                        .ToList()
                });
            }
            StatusText = $"{episodes.Count} episodes in {SeasonGroups.Count} seasons";
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load Xtream episodes", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void BuildM3uSeasonGroups(List<ChannelListItemViewModel> episodes)
    {
        var groups = episodes
            .Select(ep => (Ep: ep,
                Match: Regex.Match(ep.Name, @"S(\d{1,2})", RegexOptions.IgnoreCase)))
            .Select(x => (x.Ep, Season: x.Match.Success ? int.Parse(x.Match.Groups[1].Value) : 1))
            .GroupBy(x => x.Season)
            .OrderBy(g => g.Key);

        foreach (var g in groups)
        {
            var ordered = g.OrderBy(x => x.Ep.Name).ToList();
            var parsed = ordered
                .Select(x => (x.Ep, EpNum: TryExtractEpisodeNum(x.Ep.Name)))
                .OrderBy(x => x.EpNum ?? int.MaxValue)
                .ThenBy(x => x.Ep.Name);

            SeasonGroups.Add(new SeasonGroup
            {
                SeasonNumber = g.Key,
                Episodes = parsed.Select(x => new VodEpisodeViewModel(
                    new VodEpisode
                    {
                        Id = 0,
                        SeasonNum = g.Key,
                        EpisodeNum = x.EpNum ?? 0,
                        Title = x.Ep.Name,
                        Url = x.Ep.Url
                    },
                    x.Ep.Logo)).ToList()
            });
        }
    }

    private static int? TryExtractEpisodeNum(string name)
    {
        var m = Regex.Match(name, @"S\d{1,2}E(\d{1,3})", RegexOptions.IgnoreCase);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        m = Regex.Match(name, @"\b(\d{1,2})x(\d{1,3})\b", RegexOptions.IgnoreCase);
        if (m.Success) return int.Parse(m.Groups[2].Value);

        m = Regex.Match(name, @"\b(?:Episode|Ep|Part)\s*(\d+)\b", RegexOptions.IgnoreCase);
        if (m.Success) return int.Parse(m.Groups[1].Value);

        return null;
    }

    [RelayCommand]
    private void SelectEpisode(VodEpisodeViewModel? episode)
    {
        if (episode?.Url is null) return;
        PlayRequested?.Invoke(episode.Url);
    }

    [RelayCommand]
    private void BackToShows()
    {
        IsShowingEpisodes = false;
    }

    private static bool IsGarbageEntry(string name) =>
        name.StartsWith("ItEGr", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("ltEGr", StringComparison.OrdinalIgnoreCase);
}
