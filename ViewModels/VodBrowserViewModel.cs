using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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

    private readonly OMDbService? _omdb;
    private bool _omdbPosterRequested;

    /// <summary>Resume fraction (0..1) for the Continue Watching strip; null in the main grid.</summary>
    public double? Progress { get; set; }
    public double ProgressFraction => Progress ?? 0;

    /// <summary>Set by the Continue Watching strip so a row's context menu can remove it.</summary>
    public Action<VodMovieViewModel>? OnRemove { get; set; }

    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";
    public string YearDisplay => Year is not null ? $"({Year})" : "";

    public VodMovieViewModel(VodMovie movie, string serverUrl, string username, string password, OMDbService? omdb = null)
    {
        StreamId = movie.StreamId;
        Name = movie.Name;
        Poster = movie.Poster;
        Year = movie.Year;
        Plot = movie.Plot;
        Rating = movie.Rating;
        Url = $"{serverUrl.TrimEnd('/')}/movie/{username}/{password}/{movie.StreamId}.{movie.Extension ?? "mp4"}";
        _omdb = omdb;
        _ = ImageService.LoadInto(Poster, b => PosterSrc = b);
    }

    public VodMovieViewModel(string name, string directUrl, string? logoUrl = null, OMDbService? omdb = null)
    {
        Name = name;
        Url = directUrl;
        _omdb = omdb;
        if (logoUrl is not null)
            _ = ImageService.LoadInto(logoUrl, b => PosterSrc = b);
    }

    /// <summary>
    /// Replaces the provider poster (often wrong) with OMDb's, fetched lazily the first time
    /// the card is shown so we only spend an API request per title the user actually views.
    /// </summary>
    public void RequestOmdbPoster()
    {
        if (_omdbPosterRequested || _omdb is null) return;
        _omdbPosterRequested = true;
        _ = LoadOmdbPosterAsync();
    }

    private async Task LoadOmdbPosterAsync()
    {
        try
        {
            var result = await _omdb!.FetchAsync(Name, Year);
            await ImageService.LoadInto(result?.PosterUrl, b => PosterSrc = b);
        }
        catch (Exception ex)
        {
            LogService.Warn("OMDb poster load failed", new { Name, error = ex.Message });
        }
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
            _ = ImageService.LoadInto(logoUrl, b => PosterSrc = b);
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
    private readonly OMDbService _omdb = new(null);

    public void SetOmdbKey(string? apiKey) => _omdb.SetApiKey(apiKey);

    private List<ChannelListItemViewModel> _allM3uMovies = [];
    private List<VodMovieViewModel> _allXtreamMovies = [];

    public event Action<string>? PlayRequested;
    public event Action<VodMovieViewModel>? DetailRequested;
    public event Action<string>? RemoveFromContinueWatchingRequested;

    [ObservableProperty]
    private ObservableCollection<VodMovieViewModel> _movies = [];

    [ObservableProperty]
    private ObservableCollection<VodMovieViewModel> _continueWatching = [];

    private IReadOnlyDictionary<string, VodProgressEntry> _vodProgress = new Dictionary<string, VodProgressEntry>();

    public bool ShowContinueWatching => ShowMovies && ContinueWatching.Count > 0;

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

    public IReadOnlyList<string> SortOptions => VodSorting.Options;

    [ObservableProperty]
    private string _selectedSort = VodSorting.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMovies))]
    [NotifyPropertyChangedFor(nameof(ShowSeries))]
    [NotifyPropertyChangedFor(nameof(ShowEpisodes))]
    [NotifyPropertyChangedFor(nameof(ShowContinueWatching))]
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
    partial void OnSelectedSortChanged(string value) => ApplyMovieFilters();

    public async Task InitializeAsync(XtreamConnectionInfo connectionInfo)
    {
        _connectionInfo = connectionInfo;
        await LoadMoviesAsync();
    }

    public void InitializeFromChannels(List<ChannelListItemViewModel> vodChannels)
    {
        _connectionInfo = null;
        _allM3uMovies = DedupByTitle(vodChannels, ch => ch.Name);
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

        IEnumerable<ChannelListItemViewModel> pool = _allM3uMovies
            .Where(ch => !ChannelClassifier.IsGarbageEntry(ch.Name));
        if (cat != "All")
            pool = pool.Where(ch => (ch.Group ?? "Uncategorized").Equals(cat, StringComparison.OrdinalIgnoreCase));

        if (query.Length > 0)
            pool = pool.Where(ch => ch.Name.ToLowerInvariant().Contains(query));

        pool = VodSorting.Apply(pool, SelectedSort, _omdb, ch => ch.Name, _ => null);

        const int max = 500;
        var filtered = pool.Take(max).ToList();

        Movies.Clear();
        foreach (var ch in filtered)
            Movies.Add(new VodMovieViewModel(ch.Name, ch.Url, ch.Logo, _omdb));

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

        pool = VodSorting.Apply(pool, SelectedSort, _omdb, m => m.Name, m => m.Year);

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
            _allXtreamMovies = DedupByTitle(movies, m => m.Name)
                .Select(m => new VodMovieViewModel(
                    m, _connectionInfo.ServerUrl, _connectionInfo.Username, _connectionInfo.Password, _omdb))
                .ToList();
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
    private void ResumeMovie(VodMovieViewModel? movie)
    {
        if (movie?.Url is null) return;
        PlayRequested?.Invoke(movie.Url);
    }

    /// <summary>Rebuild the Continue Watching strip from the latest saved VOD progress.</summary>
    public void RefreshContinueWatching(IReadOnlyDictionary<string, VodProgressEntry> progress)
    {
        _vodProgress = progress;
        BuildContinueWatching();
    }

    private void BuildContinueWatching()
    {
        ContinueWatching.Clear();

        var resumable = _vodProgress
            .Where(e => e.Value.PositionMs > 30_000 && e.Value.PositionMs < e.Value.DurationMs * 0.95)
            .OrderByDescending(e => e.Value.UpdatedAt);

        const int max = 12;
        foreach (var (url, entry) in resumable)
        {
            var vm = ResolveMovie(url);
            if (vm is null) continue;
            vm.Progress = entry.DurationMs > 0 ? (double)entry.PositionMs / entry.DurationMs : null;
            vm.OnRemove = m => RemoveFromContinueWatchingRequested?.Invoke(m.Url);
            ContinueWatching.Add(vm);
            if (ContinueWatching.Count >= max) break;
        }

        OnPropertyChanged(nameof(ShowContinueWatching));
    }

    /// <summary>Find a loaded movie by URL so the strip can show its title/poster.</summary>
    private VodMovieViewModel? ResolveMovie(string url)
    {
        if (_connectionInfo is not null)
            return _allXtreamMovies.FirstOrDefault(m => m.Url == url);

        var ch = _allM3uMovies.FirstOrDefault(c => c.Url == url);
        return ch is null ? null : new VodMovieViewModel(ch.Name, ch.Url, ch.Logo, _omdb);
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

    // Quality/format/codec tokens that mark the same movie listed multiple times.
    private static readonly Regex QualityTokens = new(
        @"\b(4k|uhd|fhd|hd|sd|hq|hevc|h\.?26[45]|x26[45]|2160p|1080p|720p|480p|dolby|atmos|multi|dual|3d|imax|remux|web-?dl|webrip|bluray|bdrip|hdrip|dvdrip)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Collapses duplicate listings of one movie to a single entry, keeping the first seen.
    /// ponytail: heuristic title-normalization — strips quality tags and bare copy-counters
    /// like "(2)" but preserves sequel numbers and 4-digit years. Tune the token list if a
    /// provider's duplicates slip through, or distinct titles get wrongly merged.
    /// </summary>
    private static List<T> DedupByTitle<T>(IEnumerable<T> items, Func<T, string> nameOf)
    {
        var seen = new HashSet<string>();
        var result = new List<T>();
        foreach (var item in items)
            if (seen.Add(NormalizeTitle(nameOf(item))))
                result.Add(item);
        return result;
    }

    private static string NormalizeTitle(string name)
    {
        var s = name.ToLowerInvariant();
        s = Regex.Replace(s, @"\(\s*\d{1,2}\s*\)", " ");  // copy counters: (2), (3) — not 4-digit years
        s = QualityTokens.Replace(s, " ");
        s = Regex.Replace(s, @"[^a-z0-9]+", " ");          // keeps title numbers (sequels) and years
        return s.Trim();
    }
}

public enum VodViewMode
{
    Movies,
    Series,
    Episodes
}
