using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Services;
using Sabeltann.ViewModels;

namespace Sabeltann;

public partial class MovieDetailViewModel : ObservableObject
{
    private readonly OMDbService _omdb;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    [ObservableProperty]
    private string _title = "";

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
    private string? _localRating;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImdbStars))]
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

    public string ImdbStars => BuildStars(ImdbRating);

    public MovieDetailViewModel(OMDbService omdb)
    {
        _omdb = omdb;
    }

    public async Task LoadAsync(VodMovieViewModel movie, CancellationToken ct = default)
    {
        // Set local-data properties immediately
        Title = movie.Name;
        Year = movie.Year;
        Plot = movie.Plot;
        Director = null;
        Cast = null;
        LocalRating = movie.Rating;
        PlayUrl = movie.Url;

        IsLoading = true;

        string? omdbPosterUrl = null;

        try
        {
            var result = await _omdb.FetchAsync(movie.Name, movie.Year, ct);
            if (result is not null)
            {
                ImdbRating = result.ImdbRating;
                RottenTomatoes = result.RottenTomatoes;
                Runtime = result.Runtime;
                Genre = result.Genre;
                Director = result.Director;
                Cast = result.Actors;
                if (!string.IsNullOrEmpty(result.Plot))
                    Plot = result.Plot;
                omdbPosterUrl = result.PosterUrl;
                HasRatings = true;
            }
            else
            {
                HasRatings = false;
            }
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

        // Load poster: prefer OMDb poster, fall back to movie poster
        var posterUrl = !string.IsNullOrEmpty(omdbPosterUrl) ? omdbPosterUrl : movie.Poster;
        if (!string.IsNullOrEmpty(posterUrl))
        {
            try
            {
                var bytes = await _http.GetByteArrayAsync(posterUrl, ct);
                using var ms = new System.IO.MemoryStream(bytes);
                PosterBitmap = new Bitmap(ms);
            }
            catch (Exception ex)
            {
                LogService.Error("MovieDetailViewModel.LoadAsync poster download failed", new { posterUrl, error = ex.Message });
            }
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
