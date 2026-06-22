using System.Collections.ObjectModel;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public partial class VodMovieViewModel : ObservableObject
{
    public int StreamId { get; }
    public string Name { get; }
    public string? Poster { get; }
    public string? Year { get; }
    public string? Plot { get; }
    public string? Rating { get; }
    public string Url { get; }

    [ObservableProperty]
    private Bitmap? _posterSrc;

    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";
    public string YearDisplay => Year is not null ? $"({Year})" : "";

    public VodMovieViewModel(VodMovie movie, string serverUrl, string username, string password)
    {
        StreamId = movie.StreamId;
        Name = movie.Name;
        Poster = movie.Poster;
        Year = movie.Year;
        Plot = movie.Plot;
        Rating = movie.Rating;
        Url = $"{serverUrl.TrimEnd('/')}/movie/{username}/{password}/{movie.StreamId}.{movie.Extension ?? "mp4"}";
        _ = LoadImageAsync(Poster);
    }

    public VodMovieViewModel(string name, string directUrl, string? logoUrl = null)
    {
        Name = name;
        Url = directUrl;
        if (logoUrl is not null)
            _ = LoadImageAsync(logoUrl);
    }

    private async Task LoadImageAsync(string? url)
    {
        var bmp = await ImageService.LoadAsync(url);
        if (bmp is not null)
            PosterSrc = bmp;
    }
}

public partial class VodSeriesViewModel : ObservableObject
{
    public int SeriesId { get; }
    public string Name { get; }
    public string? Cover { get; }
    public string? Year { get; }
    public string? Plot { get; }

    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";
    public string YearDisplay => Year is not null ? $"({Year})" : "";

    public VodSeriesViewModel(VodSeriesItem series)
    {
        SeriesId = series.SeriesId;
        Name = series.Name;
        Cover = series.Cover;
        Year = series.Year;
        Plot = series.Plot;
    }
}

public partial class VodEpisodeViewModel : ObservableObject
{
    public int Id { get; }
    public int SeasonNum { get; }
    public int EpisodeNum { get; }
    public string Title { get; }

    [ObservableProperty]
    private Bitmap? _posterSrc;

    public string? Url { get; }

    public string Display => $"S{SeasonNum:D2}E{EpisodeNum:D2} - {Title}";

    public VodEpisodeViewModel(VodEpisode ep, string? logoUrl = null)
    {
        Id = ep.Id;
        SeasonNum = ep.SeasonNum;
        EpisodeNum = ep.EpisodeNum;
        Title = ep.Title;
        Url = ep.Url;
        if (logoUrl is not null)
            _ = LoadImageAsync(logoUrl);
    }

    private async Task LoadImageAsync(string? url)
    {
        var bmp = await ImageService.LoadAsync(url);
        if (bmp is not null)
            PosterSrc = bmp;
    }
}

public class SeasonGroup
{
    public int SeasonNumber { get; set; }
    public string Header => $"Season {SeasonNumber}";
    public List<VodEpisodeViewModel> Episodes { get; set; } = [];
}

public partial class VodBrowserViewModel : ObservableObject
{
    public static readonly FuncValueConverter<bool, IBrush> TabBg = new(
        active => active ? new SolidColorBrush(Color.Parse("#313244")) : Brushes.Transparent);

    private readonly XtreamService _xtream = new();
    private XtreamConnectionInfo? _connectionInfo;

    private List<ChannelListItemViewModel> _allM3uMovies = [];
    private List<VodMovieViewModel> _allXtreamMovies = [];

    public event Action<string>? PlayRequested;
    public event Action<VodMovieViewModel>? DetailRequested;

    [ObservableProperty]
    private ObservableCollection<VodMovieViewModel> _movies = [];

    [ObservableProperty]
    private ObservableCollection<VodSeriesViewModel> _series = [];

    [ObservableProperty]
    private ObservableCollection<SeasonGroup> _seasonGroups = [];

    [ObservableProperty]
    private ObservableCollection<string> _categories = [];

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMovies))]
    [NotifyPropertyChangedFor(nameof(ShowSeries))]
    [NotifyPropertyChangedFor(nameof(ShowEpisodes))]
    private VodViewMode _currentView = VodViewMode.Movies;

    public bool ShowMovies => CurrentView == VodViewMode.Movies;
    public bool ShowSeries => CurrentView == VodViewMode.Series;
    public bool ShowEpisodes => CurrentView == VodViewMode.Episodes;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentSeriesName = "";

    partial void OnSelectedCategoryChanged(string? value) => ApplyMovieFilters();
    partial void OnSearchTextChanged(string value) => ApplyMovieFilters();

    public async Task InitializeAsync(XtreamConnectionInfo connectionInfo)
    {
        _connectionInfo = connectionInfo;
        await LoadMoviesAsync();
    }

    public void InitializeFromChannels(List<ChannelListItemViewModel> vodChannels)
    {
        _connectionInfo = null;
        _allM3uMovies = vodChannels;
        IsLoading = true;
        StatusText = "Loading VOD...";
        CurrentView = VodViewMode.Movies;

        BuildCategories(vodChannels.Select(ch => ch.Group ?? "Uncategorized").ToList());
        ApplyMovieFilters();
        IsLoading = false;
    }

    private void BuildCategories(List<string> groups)
    {
        Categories.Clear();
        var sorted = groups
            .GroupBy(g => g, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .OrderBy(g => g == "Uncategorized" ? 1 : 0)
            .ThenBy(g => g)
            .ToList();
        Categories.Add("All");
        foreach (var g in sorted)
            Categories.Add(g);
        SelectedCategory = "All";
    }

    private void ApplyMovieFilters()
    {
        if (_connectionInfo is not null)
            ApplyXtreamMovieFilters();
        else
            ApplyM3uMovieFilters();
    }

    private void ApplyM3uMovieFilters()
    {
        var query = (SearchText ?? "").Trim().ToLowerInvariant();
        var cat = SelectedCategory ?? "All";

        IEnumerable<ChannelListItemViewModel> pool = _allM3uMovies;
        if (cat != "All")
            pool = pool.Where(ch => (ch.Group ?? "Uncategorized").Equals(cat, StringComparison.OrdinalIgnoreCase));

        if (query.Length > 0)
            pool = pool.Where(ch => ch.Name.ToLowerInvariant().Contains(query));

        const int max = 500;
        var filtered = pool.Take(max).ToList();

        Movies.Clear();
        foreach (var ch in filtered)
            Movies.Add(new VodMovieViewModel(ch.Name, ch.Url, ch.Logo));

        var total = pool.Count();
        StatusText = total > max
            ? $"{max} of {total} titles shown (use search to narrow)"
            : $"{Movies.Count} titles";
    }

    private void ApplyXtreamMovieFilters()
    {
        var query = (SearchText ?? "").Trim().ToLowerInvariant();

        IEnumerable<VodMovieViewModel> pool = _allXtreamMovies;
        if (query.Length > 0)
            pool = pool.Where(m => m.Name.ToLowerInvariant().Contains(query));

        const int max = 500;
        var filtered = pool.Take(max).ToList();

        Movies.Clear();
        foreach (var m in filtered)
            Movies.Add(m);

        var total = pool.Count();
        StatusText = total > max
            ? $"{max} of {total} titles shown (use search to narrow)"
            : $"{Movies.Count} titles";
    }

    [RelayCommand]
    private async Task LoadMoviesAsync()
    {
        if (_connectionInfo is null) return;
        IsLoading = true;
        StatusText = "Loading movies...";
        CurrentView = VodViewMode.Movies;
        try
        {
            var movies = await _xtream.GetVodStreamsAsync(_connectionInfo);
            _allXtreamMovies = movies.Select(m => new VodMovieViewModel(
                m, _connectionInfo.ServerUrl, _connectionInfo.Username, _connectionInfo.Password)).ToList();
            Categories.Clear();
            ApplyXtreamMovieFilters();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load VOD movies", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadSeriesAsync()
    {
        if (_connectionInfo is null) return;
        IsLoading = true;
        StatusText = "Loading series...";
        CurrentView = VodViewMode.Series;
        try
        {
            var series = await _xtream.GetSeriesAsync(_connectionInfo);
            Series.Clear();
            foreach (var s in series)
                Series.Add(new VodSeriesViewModel(s));
            StatusText = $"{Series.Count} series loaded";
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load series", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SelectSeriesAsync(VodSeriesViewModel? series)
    {
        if (series is null || _connectionInfo is null) return;
        IsLoading = true;
        StatusText = $"Loading {series.Name}...";
        CurrentSeriesName = series.Name;
        try
        {
            var episodes = await _xtream.GetSeriesEpisodesAsync(_connectionInfo, series.SeriesId);
            SeasonGroups.Clear();
            var groups = episodes
                .Select(e => new VodEpisodeViewModel(e))
                .GroupBy(e => e.SeasonNum)
                .OrderBy(g => g.Key);
            foreach (var g in groups)
            {
                SeasonGroups.Add(new SeasonGroup
                {
                    SeasonNumber = g.Key,
                    Episodes = g.OrderBy(e => e.EpisodeNum).ToList()
                });
            }
            CurrentView = VodViewMode.Episodes;
            StatusText = $"{episodes.Count} episodes in {SeasonGroups.Count} seasons";
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load episodes", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void SelectEpisode(VodEpisodeViewModel? episode)
    {
        if (episode?.Url is null) return;
        PlayRequested?.Invoke(episode.Url);
    }

    [RelayCommand]
    private void PlayMovie(VodMovieViewModel? movie)
    {
        if (movie is null) return;
        DetailRequested?.Invoke(movie);
    }

    [RelayCommand]
    private void ShowMoviesTab()
    {
        CurrentView = VodViewMode.Movies;
    }

    [RelayCommand]
    private void ShowSeriesTab()
    {
        CurrentView = VodViewMode.Series;
    }

    [RelayCommand]
    private void BackToSeries()
    {
        CurrentView = VodViewMode.Series;
    }
}

public enum VodViewMode
{
    Movies,
    Series,
    Episodes
}
