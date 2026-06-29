using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Services;
using Sabeltann.ViewModels;

namespace Sabeltann;

public partial class MovieDetailViewModel : ObservableObject
{
    private OMDbService _omdb;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>Rebuilds the OMDb client when the user changes the API key in settings.</summary>
    public void SetOmdbKey(string? apiKey) => _omdb = new OMDbService(apiKey);

    [ObservableProperty]
    private string _title = "";

    /// <summary>Set for series episodes (e.g. "S01E03 · Pilot"); null for movies.</summary>
    [ObservableProperty]
    private string? _episodeLabel;

    [ObservableProperty]
    private string? _year;

    [ObservableProperty]
    private string? _plot;

    [ObservableProperty]
    private string? _director;

    [ObservableProperty]
    private string? _cast;

    [ObservableProperty]
    private string? _runtime;

    [ObservableProperty]
    private string? _genre;

    [ObservableProperty]
    private string? _language;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stars))]
    [NotifyPropertyChangedFor(nameof(HasStars))]
    private string? _localRating;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stars))]
    [NotifyPropertyChangedFor(nameof(HasStars))]
    private string? _imdbRating;

    [ObservableProperty]
    private string? _rottenTomatoes;

    [ObservableProperty]
    private bool _hasRatings;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Bitmap? _posterBitmap;

    public string? PlayUrl { get; private set; }

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private string? _resumeText;

    public event Action? BackRequested;

    /// <summary>Sets the resume affordance from a saved position (null hides the Resume button).</summary>
    public void SetResume(long? positionMs)
    {
        CanResume = positionMs is not null;
        ResumeText = positionMs is long ms
            ? $"▶  Resume from {FormatTime(ms)}"
            : null;
    }

    private static string FormatTime(long ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1
            ? $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    /// <summary>5-star rendering from IMDb (preferred) or the provider's own rating.</summary>
    public string Stars => BuildStars(ImdbRating ?? LocalRating);
    public bool HasStars => Stars.Length > 0;

    public MovieDetailViewModel(OMDbService omdb)
    {
        _omdb = omdb;
    }

    public async Task LoadAsync(VodMovieViewModel movie, CancellationToken ct = default)
    {
        // Set local-data properties immediately
        Title = movie.Name;
        EpisodeLabel = null;
        Year = movie.Year;
        Plot = movie.Plot;
        Director = null;
        Cast = null;
        Language = null;
        LocalRating = movie.Rating;
        PlayUrl = movie.Url;

        IsLoading = true;
        string? omdbPosterUrl = null;
        try
        {
            omdbPosterUrl = ApplyOmdbResult(await _omdb.FetchAsync(movie.Name, movie.Year, ct));
        }
        catch (Exception ex)
        {
            LogService.Error("MovieDetailViewModel.LoadAsync OMDb fetch failed", new { movie.Name, error = ex.Message });
            HasRatings = false;
        }
        finally
        {
            IsLoading = false;
        }

        await LoadPosterAsync(omdbPosterUrl, movie.Poster, ct);
    }

    /// <summary>Loads a series episode card: OMDb metadata/poster are matched by the show name.</summary>
    public async Task LoadEpisodeAsync(string showName, string? year, string episodeLabel,
        string playUrl, string? fallbackPoster, CancellationToken ct = default)
    {
        Title = showName;
        EpisodeLabel = episodeLabel;
        Year = year;
        Plot = null;
        Director = null;
        Cast = null;
        Language = null;
        Genre = null;
        Runtime = null;
        ImdbRating = null;
        RottenTomatoes = null;
        LocalRating = null;
        HasRatings = false;
        PosterBitmap = null;
        PlayUrl = playUrl;

        IsLoading = true;
        string? omdbPosterUrl = null;
        try
        {
            omdbPosterUrl = ApplyOmdbResult(await _omdb.FetchAsync(showName, year, ct));
        }
        catch (Exception ex)
        {
            LogService.Error("MovieDetailViewModel.LoadEpisodeAsync OMDb fetch failed", new { showName, error = ex.Message });
            HasRatings = false;
        }
        finally
        {
            IsLoading = false;
        }

        await LoadPosterAsync(omdbPosterUrl, fallbackPoster, ct);
    }

    /// <summary>Applies OMDb fields and returns its poster URL (null when there's no result).</summary>
    private string? ApplyOmdbResult(OMDbResult? result)
    {
        if (result is null)
        {
            HasRatings = false;
            return null;
        }
        ImdbRating = result.ImdbRating;
        RottenTomatoes = result.RottenTomatoes;
        Runtime = result.Runtime;
        Genre = result.Genre;
        Director = result.Director;
        Cast = result.Actors;
        Language = result.Language;
        if (!string.IsNullOrEmpty(result.Plot))
            Plot = result.Plot;
        HasRatings = true;
        return result.PosterUrl;
    }

    private async Task LoadPosterAsync(string? omdbPosterUrl, string? fallback, CancellationToken ct)
    {
        var posterUrl = !string.IsNullOrEmpty(omdbPosterUrl) ? omdbPosterUrl : fallback;
        if (string.IsNullOrEmpty(posterUrl)) return;
        try
        {
            var bytes = await _http.GetByteArrayAsync(posterUrl, ct);
            using var ms = new System.IO.MemoryStream(bytes);
            PosterBitmap = new Bitmap(ms);
        }
        catch (Exception ex)
        {
            LogService.Error("MovieDetailViewModel poster download failed", new { posterUrl, error = ex.Message });
        }
    }

    [RelayCommand]
    private void Back() => BackRequested?.Invoke();

    private static string BuildStars(string? rating)
    {
        if (!double.TryParse(rating, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r)) return "";
        var full = (int)Math.Round(r / 2);   // scale 0-10 to 0-5 stars
        return new string('★', full) + new string('☆', 5 - full);
    }
}
