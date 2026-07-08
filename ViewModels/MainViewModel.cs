using System.Collections.ObjectModel;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;
using Sabeltann;
using Sabeltann.Views;
using Sharpcaster.Models;

namespace Sabeltann.ViewModels;

public enum ContentMode
{
    Welcome,
    Picker,
    LiveTv,
    Movies,
    Series,
    MovieDetail
}

/// <summary>Whether a cast is active. Libvlc = casting via libvlc's chromecast output (by IP).</summary>
public enum CastMode { None, Libvlc }

public partial class MainViewModel : ObservableObject
{
    // Transport icons as vector geometry (Material Design Icons, 24x24 viewbox) instead of
    // emoji/symbol text glyphs, which font-fall-back to tofu ("yellow square") or hamburger
    // shapes depending on the installed font. PathIcon fills these with its Foreground brush.
    public static readonly Geometry PlayGeo = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z");
    public static readonly Geometry PauseGeo = Geometry.Parse("M14,19H18V5H14M6,19H10V5H6V19Z");
    public static readonly Geometry StopGeo = Geometry.Parse("M18,18H6V6H18V18Z");
    public static readonly Geometry RewindGeo = Geometry.Parse("M11.5,12L20,18V6M11,18V6L2.5,12L11,18Z");
    public static readonly Geometry ForwardGeo = Geometry.Parse("M13,6V18L21.5,12M4,18L12.5,12L4,6V18Z");
    public static readonly Geometry VolumeHighGeo = Geometry.Parse("M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.84 14,18.7V20.77C18,19.86 21,16.28 21,12C21,7.72 18,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,15.29 16.5,13.76 16.5,12M3,9V15H7L12,20V4L7,9H3Z");
    public static readonly Geometry VolumeOffGeo = Geometry.Parse("M12,4L9.91,6.09L12,8.18M4.27,3L3,4.27L7.73,9H3V15H7L12,20V13.27L16.25,17.53C15.58,18.04 14.83,18.46 14,18.7V20.77C15.38,20.45 16.63,19.82 17.68,18.96L19.73,21L21,19.73L12,10.73V10.18L16.45,12.63C16.5,12.43 16.5,12.21 16.5,12C16.5,10.23 15.5,8.71 14,7.97V10.18M19,12C19,12.94 18.8,13.82 18.46,14.64L19.97,16.15C20.62,14.91 21,13.5 21,12C21,7.72 18,4.14 14,3.23V5.29C16.89,6.15 19,8.83 19,12Z");
    public static readonly Geometry DebugGeo = Geometry.Parse("M3.5,18.49L9.5,12.48L13.5,16.48L22,6.92L20.59,5.51L13.5,13.5L9.5,9.5L2,17L3.5,18.49Z");
    public static readonly Geometry PopoutGeo = Geometry.Parse("M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z");
    public static readonly Geometry FullscreenGeo = Geometry.Parse("M5,5H10V7H7V10H5V5M14,5H19V10H17V7H14V5M17,14H19V19H14V17H17V14M10,17V19H5V14H7V17H10Z");

    public static readonly FuncValueConverter<bool, Geometry> PlayPauseIcon = new(
        playing => playing ? PauseGeo : PlayGeo);

    public static readonly FuncValueConverter<bool, Geometry> MuteIcon = new(
        muted => muted ? VolumeOffGeo : VolumeHighGeo);

    public static readonly FuncValueConverter<bool, IBrush> CastIcon = new(
        casting => casting ? new SolidColorBrush(Color.Parse("#a6e3a1")) : new SolidColorBrush(Color.Parse("#89b4fa")));

    public static readonly FuncValueConverter<bool, IBrush> ConnDot = new(
        connected => connected ? new SolidColorBrush(Color.Parse("#a6e3a1")) : new SolidColorBrush(Color.Parse("#6c7086")));

    public static readonly FuncValueConverter<string, IBrush> HashColor = new(name =>
    {
        if (string.IsNullOrEmpty(name))
            return new SolidColorBrush(Color.Parse("#313244"));
        var h = (uint)Math.Abs(name.GetHashCode()) % 6;
        var colors = new[] { "#cba6f7", "#89b4fa", "#a6e3a1", "#f9e2af", "#f38ba8", "#94e2d5" };
        return new SolidColorBrush(Color.Parse(colors[h]));
    });

    public static readonly FuncValueConverter<string, string> FirstChar = new(name =>
        string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant());

    public static readonly FuncValueConverter<object?, bool> ImgFallback = new(
        value => value is null);

    private readonly M3UParser _parser = new();
    private readonly XtreamService _xtream = new();
    private readonly SettingsService _settings = new();
    private readonly ChannelCacheService _cache = new();
    private readonly UpdateService _updateService = new();
    private readonly System.Timers.Timer _updateCheckTimer;
    private SettingsData _settingsData = new();
    private DispatcherTimer? _positionTimer;
    private PlaybackService? _player;
    private List<ChannelListItemViewModel> _allChannels = [];
    private List<ChannelListItemViewModel> _liveChannels = [];
    private List<ChannelListItemViewModel> _movieChannels = [];
    private List<ChannelListItemViewModel> _seriesChannels = [];

    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> _categories = [];

    [ObservableProperty]
    private ObservableCollection<ChannelListItemViewModel> _filteredChannels = [];

    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPlayingTitle))]
    private ChannelListItemViewModel? _selectedChannel;

    /// <summary>Public LIVE-vs-VOD flag for the playback toolbar badge.</summary>
    public bool IsCurrentVod => _isCurrentVod;

    /// <summary>Title shown in the playback toolbar (channel name for live, movie/episode title for VOD).</summary>
    public string CurrentPlayingTitle => _isCurrentVod ? (MovieDetail?.Title ?? "") : (SelectedChannel?.Name ?? "");

    public string ConnectionServerUrl => _pendingXtreamInfo?.ServerUrl ?? _settings.Load().LastXtream?.ServerUrl ?? "";
    public string ConnectionUsername => _pendingXtreamInfo?.Username ?? _settings.Load().LastXtream?.Username ?? "";

    public SettingsData GetSettings() => _settings.Load();

    public void ApplySettings(SettingsData data)
    {
        _settings.Save(data);
        _settingsData = data;
        _updateService.IncludePrerelease = data.IncludePrerelease;
        Volume = data.DefaultVolume;
        ApplyOmdbKey(data.OmdbApiKey);
    }

    private void ApplyOmdbKey(string? key)
    {
        MovieDetail.SetOmdbKey(key);
        VodBrowser.SetOmdbKey(key);
        SeriesBrowser.SetOmdbKey(key);
    }

    public void SetSearchResults(string query)
    {
        SearchText = query;
        var q = query.Trim().ToLowerInvariant();

        FilteredChannels.Clear();
        Categories.Clear();

        if (string.IsNullOrEmpty(q))
        {
            if (Mode == ContentMode.LiveTv)
            {
                ShowLiveChannels();
            }
            return;
        }

        var pool = Mode switch
        {
            ContentMode.Movies => _movieChannels,
            ContentMode.Series => _seriesChannels,
            _ => _liveChannels.Count > 0 ? _liveChannels : _allChannels
        };

        var matches = pool
            .Where(c => c.Name.ToLowerInvariant().Contains(q))
            .Take(500)
            .ToList();

        if (matches.Count == 0) { StatusText = $"No results for \"{query}\""; return; }

        foreach (var ch in matches)
            ch.EnsureImageLoaded();

        var cat = new CategoryViewModel { Name = $"Search: \"{query}\" ({matches.Count})" };
        cat.Channels.AddRange(matches);
        Categories.Add(cat);
        SelectedCategory = cat;
        StatusText = $"{matches.Count} results for \"{query}\"";
    }

    // Morphing-toolbar segmented tabs. Movies/Series need their async browser init,
    // not a bare Mode set — reuse the existing browser entrypoints.
    [RelayCommand] private void SwitchToLive() { SaveSection("live"); ShowLiveChannels(); }
    [RelayCommand] private Task SwitchToMovies() { SaveSection("movies"); return ShowMoviesBrowserAsync(); }
    [RelayCommand] private Task SwitchToSeries() { SaveSection("series"); return ShowSeriesBrowserAsync(); }

    private void SaveSection(string section) => _settingsData = MergeAndSave(s => s.LastSection = section);

    // Land straight in the last-used section after connecting — the old content-picker screen was
    // redundant since the top tabs switch sections anyway.
    private async Task AutoLaunchAsync()
    {
        await ShowPlaylistContentAsync();
        switch (_settingsData.LastSection)
        {
            case "movies": await ShowMoviesBrowserAsync(); break;
            case "series": await ShowSeriesBrowserAsync(); break;
            default: ShowLiveChannels(); break;
        }
    }

    public void ShowLiveChannels()
    {
        Mode = ContentMode.LiveTv;
        IsBrowsing = true;
        SelectedCategory = null;
        SelectedChannel = null;
        _player?.Stop();
        IsPlaying = false;
        ChannelCount = _liveChannels.Count;
        Categories.Clear();
        var groups = _liveChannels
            .GroupBy(ch => ch.Group ?? "Uncategorized")
            .OrderBy(g => g.Key == "Uncategorized" ? 1 : 0)
            .ThenBy(g => g.Key);
        foreach (var g in groups)
        {
            var cat = new CategoryViewModel { Name = g.Key };
            cat.Channels.AddRange(g);
            Categories.Add(cat);
        }
        if (Categories.Count > 0)
            SelectedCategory = Categories[0];
    }

    public async Task ShowMoviesBrowserAsync()
    {
        VodBrowser.PlayRequested -= OnVodPlayRequested;
        VodBrowser.PlayRequested += OnVodPlayRequested;
        VodBrowser.DetailRequested -= OnMovieDetailRequested;
        VodBrowser.DetailRequested += OnMovieDetailRequested;
        VodBrowser.RemoveFromContinueWatchingRequested -= OnRemoveFromContinueWatching;
        VodBrowser.RemoveFromContinueWatchingRequested += OnRemoveFromContinueWatching;
        Mode = ContentMode.Movies;

        if (_pendingXtreamInfo is not null)
        {
            await VodBrowser.InitializeAsync(_pendingXtreamInfo);
        }
        else
        {
            VodBrowser.InitializeFromChannels(_movieChannels);
        }

        VodBrowser.RefreshContinueWatching(_settingsData.VodProgress);
    }

    // Where back/stop should return after the detail card — Movies or Series.
    private ContentMode _detailReturnMode = ContentMode.Movies;

    private void OnMovieDetailRequested(VodMovieViewModel movie)
    {
        _detailReturnMode = ContentMode.Movies;
        Mode = ContentMode.MovieDetail;
        _ = MovieDetail.LoadAsync(movie);
        MovieDetail.SetResume(TryGetResumeMs(movie.Url));
    }

    private void OnEpisodeDetailRequested(EpisodeDetail ep)
    {
        _detailReturnMode = ContentMode.Series;
        Mode = ContentMode.MovieDetail;
        _ = MovieDetail.LoadEpisodeAsync(ep.ShowName, ep.Year, ep.Label, ep.Url, ep.Poster);
        MovieDetail.SetResume(TryGetResumeMs(ep.Url));
    }

    [RelayCommand]
    private void PlayFromStart()
    {
        if (MovieDetail.PlayUrl is string url)
        {
            Mode = _detailReturnMode;
            PlayVod(url, resume: false);
        }
    }

    [RelayCommand]
    private void ResumeFromDetail()
    {
        if (MovieDetail.PlayUrl is string url)
        {
            Mode = _detailReturnMode;
            PlayVod(url, resume: true);
        }
    }

    /// <summary>Returns the saved resume position for a VOD url, or null if none is worth resuming.</summary>
    private long? TryGetResumeMs(string? url)
    {
        if (url is not null && _settingsData.VodProgress.TryGetValue(url, out var saved)
            && saved.PositionMs > 30_000 && saved.PositionMs < saved.DurationMs * 0.95)
            return saved.PositionMs;
        return null;
    }

    public async Task ShowSeriesBrowserAsync()
    {
        SeriesBrowser.EpisodeDetailRequested -= OnEpisodeDetailRequested;
        SeriesBrowser.EpisodeDetailRequested += OnEpisodeDetailRequested;
        Mode = ContentMode.Series;

        if (_pendingXtreamInfo is not null)
        {
            await SeriesBrowser.InitializeFromXtreamAsync(_pendingXtreamInfo);
        }
        else
        {
            SeriesBrowser.InitializeFromEpisodes(_seriesChannels);
        }
    }

    // Grid/episode play auto-resumes; the detail page passes an explicit choice via PlayVod.
    private void OnVodPlayRequested(string url) => PlayVod(url, resume: true);

    // Drop a saved VOD position (right-click → Remove on the Continue Watching strip).
    private void OnRemoveFromContinueWatching(string url)
    {
        LogService.Info("Remove from Continue Watching", new { url });
        _settingsData = MergeAndSave(s => s.VodProgress.Remove(url));
        VodBrowser.RefreshContinueWatching(_settingsData.VodProgress);
    }

    private void PlayVod(string url, bool resume)
    {
        try
        {
            SaveVodProgress();           // persist the title we're leaving
            _isCurrentVod = true;
            OnPropertyChanged(nameof(IsCurrentVod));
            OnPropertyChanged(nameof(CurrentPlayingTitle));
            _resumeToMs = resume ? TryGetResumeMs(url) ?? 0 : 0;
            _currentPlayingUrl = url;
            _vodPausePosition = TimeSpan.Zero;
            LogService.Info("Playing VOD", new { url });
            ShowConnectionOverlay = true;
            ShowBufferingOverlay = true;
            ConnectionState = "Connecting...";
            ConnectionProgress = 0;
            _awaitingPlayback = true;
            _awaitingSince = DateTime.UtcNow;
            _playbackRequested = true;
            _playbackConfirmed = false;
            _playStartFrames = _player?.FramesDecoded ?? 0;
            _player?.ClearBitmap();
            _player?.Play(url);
            DebugStats.SetUrl(url);
            IsPlaying = true;
            StatusText = "Playing VOD content";
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to play VOD", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
    }

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _volume = 100;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VodPositionPercent))]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    private double _vodPosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VodPositionPercent))]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    private double _vodDuration = 1;

    public double VodPositionPercent
    {
        get => VodDuration > 0 ? Math.Clamp(VodPosition / VodDuration, 0, 1) : 0;
        set
        {
            var pos = value * VodDuration;
            _player?.SetPositionPercent((float)value);
            VodPosition = pos;
        }
    }

    public string PositionText => FormatTime(VodPosition);
    public string DurationText => FormatTime(VodDuration);

    private static string FormatTime(double ms)
    {
        if (ms <= 0) return "--:--";
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1 ? $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    [RelayCommand]
    private void Seek(double percent)
    {
        VodPositionPercent = percent;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOverlay))]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(ShowVideo))]
    [NotifyPropertyChangedFor(nameof(ShowPlaybackBar))]
    [NotifyPropertyChangedFor(nameof(ShowLiveBar))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesBar))]
    [NotifyPropertyChangedFor(nameof(ShowSeriesBar))]
    [NotifyPropertyChangedFor(nameof(ShowDetailBar))]
    [NotifyPropertyChangedFor(nameof(ShowToolbarBack))]
    [NotifyPropertyChangedFor(nameof(ShowFileMenu))]
    private bool _isPlaying;

    public bool ShowOverlay => IsPlaying;

    public bool ShowVideo => IsPlaying || IsPaused;

    // The center pause overlay lives in whichever window holds the video: the main window when inline,
    // the popout when detached. Bind main to this, popout to IsPaused. (popout bug)
    public bool ShowPauseOverlay => IsPaused && !IsPoppedOut;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPauseOverlay))]
    private bool _isPoppedOut;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVideo))]
    [NotifyPropertyChangedFor(nameof(ShowPauseOverlay))]
    [NotifyPropertyChangedFor(nameof(ShowPlaybackBar))]
    [NotifyPropertyChangedFor(nameof(ShowLiveBar))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesBar))]
    [NotifyPropertyChangedFor(nameof(ShowSeriesBar))]
    [NotifyPropertyChangedFor(nameof(ShowDetailBar))]
    [NotifyPropertyChangedFor(nameof(ShowToolbarBack))]
    [NotifyPropertyChangedFor(nameof(ShowFileMenu))]
    private bool _isPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome))]
    [NotifyPropertyChangedFor(nameof(IsPicker))]
    [NotifyPropertyChangedFor(nameof(IsLiveTv))]
    [NotifyPropertyChangedFor(nameof(IsMovies))]
    [NotifyPropertyChangedFor(nameof(IsSeries))]
    [NotifyPropertyChangedFor(nameof(IsMovieDetail))]
    [NotifyPropertyChangedFor(nameof(IsVodContent))]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(ShowTimeline))]
    [NotifyPropertyChangedFor(nameof(IsCategoryBarVisible))]
    [NotifyPropertyChangedFor(nameof(ShowBackButton))]
    [NotifyPropertyChangedFor(nameof(ShowCategoryBar))]
    [NotifyPropertyChangedFor(nameof(ShowLiveBar))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesBar))]
    [NotifyPropertyChangedFor(nameof(ShowSeriesBar))]
    [NotifyPropertyChangedFor(nameof(ShowDetailBar))]
    [NotifyPropertyChangedFor(nameof(ShowToolbarBack))]
    [NotifyPropertyChangedFor(nameof(ShowFileMenu))]
    private ContentMode _mode = ContentMode.Welcome;

    public bool IsWelcome => Mode == ContentMode.Welcome;
    public bool IsPicker => Mode == ContentMode.Picker;
    public bool IsLiveTv => Mode == ContentMode.LiveTv;
    public bool IsMovies => Mode == ContentMode.Movies;
    public bool IsSeries => Mode == ContentMode.Series;
    public bool IsMovieDetail => Mode == ContentMode.MovieDetail;
    public bool IsVodContent => Mode is ContentMode.Movies or ContentMode.Series;
    public bool ShowChannelGrid => Mode == ContentMode.LiveTv && IsBrowsing && !IsTimeline;
    public bool ShowTimeline => Mode == ContentMode.LiveTv && IsBrowsing && IsTimeline;
    public bool CanUseTimeline => _activeXtreamInfo is not null;
    public bool IsCategoryBarVisible => Mode == ContentMode.LiveTv;
    public bool ShowBackButton => Mode is not ContentMode.Welcome and not ContentMode.Picker;
    public bool ShowCategoryBar => Mode is ContentMode.Welcome or ContentMode.Picker or ContentMode.LiveTv;

    // Morphing-toolbar panel gating: a browsing bar shows only when its mode is active AND video isn't playing.
    public bool ShowPlaybackBar => ShowVideo;
    public bool ShowLiveBar => IsLiveTv && !ShowVideo;
    public bool ShowMoviesBar => IsMovies && !ShowVideo;
    public bool ShowSeriesBar => IsSeries && !ShowVideo;
    public bool ShowDetailBar => IsMovieDetail && !ShowVideo;

    // Toolbar left zone: back arrow during detail/playback, ☰ File menu everywhere else.
    public bool ShowToolbarBack => ShowVideo || IsMovieDetail;
    public bool ShowFileMenu => !ShowToolbarBack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(ShowTimeline))]
    private bool _isBrowsing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(ShowTimeline))]
    private bool _isTimeline;

    [ObservableProperty]
    private int _channelCount;

    [ObservableProperty]
    private bool _showSubtitlePopup;

    public ObservableCollection<SubtitleTrackItem> SubtitleTrackItems { get; } = [];

    [ObservableProperty]
    private int _currentSubtitleTrack = -1;

    // ponytail: audio tracks reuse SubtitleTrackItem — same (id, name) shape.
    public ObservableCollection<SubtitleTrackItem> AudioTrackItems { get; } = [];

    [ObservableProperty]
    private int _currentAudioTrack = -1;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _connectionState = "";

    [ObservableProperty]
    private int _connectionProgress;

    [ObservableProperty]
    private bool _showConnectionOverlay;

    [ObservableProperty]
    private bool _showBufferingOverlay;

    public VodBrowserViewModel VodBrowser { get; } = new();
    public SeriesBrowserViewModel SeriesBrowser { get; } = new();
    public EpgTimelineViewModel EpgTimeline { get; } = new();
    public MovieDetailViewModel MovieDetail { get; } = new(new OMDbService(null));

    private XtreamConnectionInfo? _activeXtreamInfo;

    private XtreamConnectionInfo? _pendingXtreamInfo;
    private M3UPlaylist? _pendingPlaylist;
    private string? _currentPlayingUrl;
    private TimeSpan _vodPausePosition;
    private bool _awaitingPlayback;
    private DateTime _awaitingSince;
    private bool _playbackRequested;
    private bool _playbackConfirmed;
    private int _playStartFrames;
    private bool _isCurrentVod;
    private double _resumeToMs;
    private const double PlaybackTimeoutSeconds = 15;

    public event Action? ToggleFullscreenRequested;

    [ObservableProperty]
    private bool _showDebugOverlay;

    [ObservableProperty]
    private bool _showVolumePopup;

    [ObservableProperty]
    private bool _isCasting;

    [ObservableProperty]
    private string? _castTargetName;

    private CastService? _cast;
    private CastMode _castMode = CastMode.None;
    private IReadOnlyList<ChromecastReceiver> _castReceivers = [];

    // Native receivers when discovered, else the libvlc renderer names as a fallback source.
    public IReadOnlyList<string> CastTargets =>
        _castReceivers.Count > 0 ? _castReceivers.Select(r => r.Name).ToList() : (_player?.CastTargets ?? []);

    /// <summary>Wire in the Cast device discovery and kick it off.</summary>
    public void SetCastService(CastService cast)
    {
        _cast = cast;
        _ = RefreshCastDevicesAsync();
    }

    /// <summary>Re-run native device discovery — called each time the cast menu opens, since the
    /// SharpCaster scan is one-shot (unlike libvlc's continuous discoverer). Without this a first
    /// empty scan would stay empty until app restart.</summary>
    public void RescanCastDevices() => _ = RefreshCastDevicesAsync();

    private async Task RefreshCastDevicesAsync()
    {
        if (_cast is null) return;
        try
        {
            _castReceivers = await _cast.FindDevicesAsync(TimeSpan.FromSeconds(5));
            LogService.Info("Native cast discovery", new { count = _castReceivers.Count });
            Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(CastTargets)));
        }
        catch (Exception ex)
        {
            LogService.Warn("Native cast discovery failed", new { error = ex.Message });
        }
    }

    [ObservableProperty]
    private double _updateDownloadProgress;

    [ObservableProperty]
    private bool _isUpdatePending;

    [ObservableProperty]
    private string? _updateBadgeText;

    public WriteableBitmap? VideoBitmap => _player?.VideoBitmap;

    public DebugStatsViewModel DebugStats { get; }

    public void SetPlayer(PlaybackService player)
    {
        _player = player;
        _player.StartCastDiscovery();
        _player.CastTargetsChanged += (_, _) =>
            Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(CastTargets)));
        _player.Error += (_, msg) =>
        {
            LogService.Info("VLC Error event fired", new { msg, isPlaying = IsPlaying, isPaused = IsPaused });
            StatusText = $"Playback error: {msg}";
            ConnectionState = msg;
            ConnectionProgress = 0;
            IsPlaying = false;
            IsPaused = false;
            ShowConnectionOverlay = false;
            ShowBufferingOverlay = true;
        };
        _player.PlayingStarted += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            // Playing fires when the connection opens, before any frame decodes — keep the loading
            // overlay up until a real frame renders (cleared in the position timer) so dead streams
            // don't flash to a black screen. Same path for Live TV and VOD.
            _awaitingPlayback = false;
            if (!_playbackConfirmed) ConnectionState = "Buffering...";
            var len = _player.Length;
            if (len > 0) VodDuration = len;
            if (_resumeToMs > 0)
            {
                _player.SetPosition(TimeSpan.FromMilliseconds(_resumeToMs));
                StatusText = $"Resuming from {FormatTime(_resumeToMs)}";
                _resumeToMs = 0;
            }
        });
        _player.Buffering += (_, pct) => Dispatcher.UIThread.Post(() =>
        {
            ConnectionProgress = pct;
            // VLC emits Buffering both before AND after Playing (seeks, refills). 100 = done, but
            // only clear once playback is confirmed (a frame rendered) — on the initial load a dead
            // stream can report 100 with no frames, so keep the bar up until the timer confirms or
            // the watchdog fails. Mid-stream refills are already confirmed, so they clear as before.
            if (pct >= 100)
            {
                if (_playbackConfirmed)
                {
                    ConnectionState = "";
                    ShowBufferingOverlay = false;
                }
                else
                {
                    ConnectionState = "Buffering...";
                }
            }
            else
            {
                ConnectionState = $"Buffering... {pct}%";
                ShowBufferingOverlay = true;
            }
        });
        _player.Stopped += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            if (_awaitingPlayback)
            {
                // Stream failed before playing — keep overlay visible with error
                ConnectionState = "Stream unavailable";
                ShowBufferingOverlay = true;
                _awaitingPlayback = false;
            }
            else
            {
                ConnectionState = "";
                ShowConnectionOverlay = false;
                ShowBufferingOverlay = false;
                ConnectionProgress = 0;
            }
        });
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += (_, _) =>
        {
            if (_player is not null && (IsPlaying || IsPaused))
            {
                var len = _player.Length;
                if (len > 0) VodDuration = len;
                VodPosition = _player.TimeMs;
            }
            // Confirm playback once it's really up — a decoded frame locally; while casting, output goes
            // to the TV (libvlc's chromecast sout drives it), so trust the player being in the Playing
            // state. Single point that clears the loading bar for both local and cast.
            if (!_playbackConfirmed && _player is not null && (IsPlaying || IsPaused))
            {
                bool started = _castMode == CastMode.Libvlc ? _player.IsPlaying : _player.FramesDecoded > _playStartFrames;
                if (started)
                {
                    _playbackConfirmed = true;
                    ConnectionState = "";
                    ShowConnectionOverlay = false;
                    ShowBufferingOverlay = false;
                    ConnectionProgress = 0;
                }
            }
            // Watchdog: dead streams often fire Playing (connection opened) but never decode a single
            // video frame, so event-based signals can't catch them. Skip it while casting — the stream
            // decodes on the TV, so there is no reliable *local* liveness signal (input stats don't grow
            // the usual way through the chromecast sout); a false "dead" here would kill a healthy cast.
            if (_castMode != CastMode.Libvlc
                && _playbackRequested && (DateTime.UtcNow - _awaitingSince).TotalSeconds > PlaybackTimeoutSeconds)
            {
                _playbackRequested = false;
                bool dead = _player is null || _player.FramesDecoded <= _playStartFrames;
                if (dead)
                {
                    LogService.Warn("Playback watchdog: stream unavailable", new { casting = IsCasting });
                    _player?.Stop();
                    EndCasting();
                    _awaitingPlayback = false;
                    ConnectionState = "Stream unavailable";
                    ShowConnectionOverlay = false;
                    ShowBufferingOverlay = true;
                    IsPlaying = false;
                }
            }
            if (_player?.VideoBitmap is not null)
                OnPropertyChanged(nameof(VideoBitmap));
        };
        _positionTimer.Start();
        DebugStats.SetPlayer(player, this);
    }

    public MainViewModel()
    {
        DebugStats = new DebugStatsViewModel(null);
        MovieDetail.BackRequested += () => Mode = _detailReturnMode;
        EpgTimeline.PlayChannelRequested += ch => { IsTimeline = false; EpgTimeline.StopClock(); SelectedChannel = ch; };

        _updateService.UpdateReady += version =>
        {
            IsUpdatePending = true;
            UpdateBadgeText = "● Update ready";
            ShowUpdateDialog(version);
        };

        _updateCheckTimer = new System.Timers.Timer(TimeSpan.FromHours(6).TotalMilliseconds);
        _updateCheckTimer.Elapsed += async (_, _) =>
        {
            if (_settingsData.CheckForUpdatesEnabled && !_updateService.IsUpdateAvailable)
                await _updateService.CheckAndDownloadAsync();
        };
        _updateCheckTimer.AutoReset = true;
        _updateCheckTimer.Start();
    }

    /// <summary>
    /// Load the guide when the timeline is switched on. The toggle button's IsChecked two-way binding
    /// already flipped <see cref="IsTimeline"/> before this runs, so we only react to the new state.
    /// </summary>
    [RelayCommand]
    private async Task ToggleTimeline()
    {
        if (_activeXtreamInfo is null) { IsTimeline = false; return; }
        if (IsTimeline)
            await EpgTimeline.LoadAsync(_activeXtreamInfo, _liveChannels);
        else
            EpgTimeline.StopClock();
    }

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        if (value is null) return;
        ApplyFilters();
        if (Mode == ContentMode.LiveTv && !value.Name.StartsWith("Search:", StringComparison.OrdinalIgnoreCase))
            MergeAndSave(s => s.LastCategoryName = value.Name);
    }

    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilters();

    partial void OnSearchTextChanged(string value)
    {
        if (Mode != ContentMode.LiveTv) return;

        if (string.IsNullOrWhiteSpace(value))
        {
            ApplyFilters();
            return;
        }

        if (SelectedCategory is null)
            SetSearchResults(value);
        else
            ApplyFilters();
    }

    private void ApplyFilters()
    {
        var category = SelectedCategory;
        if (category is null) return;

        var query = (SearchText ?? "").Trim().ToLowerInvariant();
        const int displayLimit = 500;

        var filtered = new List<ChannelListItemViewModel>();
        int totalMatching = 0;
        foreach (var ch in category.Channels)
        {
            if (ShowFavoritesOnly && !ch.IsFavorite) continue;
            if (query.Length > 0 && !ch.Name.ToLowerInvariant().Contains(query)) continue;
            totalMatching++;
            if (filtered.Count < displayLimit)
                filtered.Add(ch);
        }

        foreach (var ch in filtered)
            ch.EnsureImageLoaded();

        FilteredChannels.Clear();
        foreach (var ch in filtered)
            FilteredChannels.Add(ch);

        if (totalMatching > filtered.Count)
            StatusText = $"Showing {filtered.Count} of {totalMatching} (use search to narrow)";
        else if (query.Length > 0)
            StatusText = $"{filtered.Count} matches";
        else
            StatusText = $"{filtered.Count} channels";
    }

    partial void OnSelectedChannelChanged(ChannelListItemViewModel? value)
    {
        try
        {
            if (value is not null && _player is not null && !string.IsNullOrEmpty(value.Url))
            {
                IsBrowsing = false;
                SaveVodProgress();
                _isCurrentVod = false;
                OnPropertyChanged(nameof(IsCurrentVod));
                OnPropertyChanged(nameof(CurrentPlayingTitle));
                _resumeToMs = 0;
                _currentPlayingUrl = value.Url;
                _vodPausePosition = TimeSpan.Zero;
                _player.SetVolume(Volume);
                LogService.Info("Playing channel", new { name = value.Name, url = value.Url });
                ShowConnectionOverlay = true;
                ShowBufferingOverlay = true;
                ConnectionState = "Connecting...";
                ConnectionProgress = 0;
                _awaitingPlayback = true;
                _awaitingSince = DateTime.UtcNow;
                _playbackRequested = true;
                _playbackConfirmed = false;
                _playStartFrames = _player?.FramesDecoded ?? 0;
                _player?.ClearBitmap();
                _player.Play(value.Url);
                DebugStats.SetUrl(value.Url);
                IsPlaying = true;
                IsPaused = false;
                SubtitleTrackItems.Clear();
                SubtitleTrackItems.Add(new SubtitleTrackItem(-1, "Off"));
                foreach (var t in _player.GetSubtitleTracks())
                    SubtitleTrackItems.Add(new SubtitleTrackItem(t.Id, t.Name));
                CurrentSubtitleTrack = _player.CurrentSubtitleTrack;
                AudioTrackItems.Clear();
                foreach (var t in _player.GetAudioTracks())
                    AudioTrackItems.Add(new SubtitleTrackItem(t.Id, t.Name));
                CurrentAudioTrack = _player.CurrentAudioTrack;
                StatusText = $"Playing: {value.Name}";
                MergeAndSave(s => s.LastChannelUrl = value.Url);
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to play channel", new { name = value?.Name, error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleSubtitlePopup()
    {
        ShowSubtitlePopup = !ShowSubtitlePopup;
    }

    public void RefreshSubtitleTracks()
    {
        if (_player is null || !IsPlaying) return;
        SubtitleTrackItems.Clear();
        SubtitleTrackItems.Add(new SubtitleTrackItem(-1, "Off"));
        foreach (var t in _player.GetSubtitleTracks())
            SubtitleTrackItems.Add(new SubtitleTrackItem(t.Id, t.Name));
    }

    [RelayCommand]
    private void SelectSubtitle(int id)
    {
        if (_player is null) return;
        if (IsCasting)
        {
            if (_isCurrentVod) _resumeToMs = _player.TimeMs;   // resume where we are after the cast rebuild
            _player.SetCastSubtitleTrack(id);
            BeginCastWindow();
        }
        else
        {
            _player.SetSubtitleTrack(id);
        }
        CurrentSubtitleTrack = id;
        ShowSubtitlePopup = false;
    }

    public void RefreshAudioTracks()
    {
        if (_player is null || !IsPlaying) return;
        AudioTrackItems.Clear();
        foreach (var t in _player.GetAudioTracks())
            AudioTrackItems.Add(new SubtitleTrackItem(t.Id, t.Name));
        CurrentAudioTrack = _player.CurrentAudioTrack;
    }

    [RelayCommand]
    private void SelectAudio(int id)
    {
        if (_player is null) return;
        if (IsCasting)
        {
            if (_isCurrentVod) _resumeToMs = _player.TimeMs;
            _player.SetCastAudioTrack(id);
            BeginCastWindow();
        }
        else
        {
            _player.SetAudioTrack(id);
        }
        CurrentAudioTrack = id;
    }

    [RelayCommand]
    private void CastTo(int index)
    {
        if (_player is null || _currentPlayingUrl is null) return;
        if (index < 0 || index >= _castReceivers.Count) return;

        // Cast via libvlc's own chromecast output, addressed by the device's IP (from the unicast scan,
        // so no mDNS needed). libvlc connects, launches the receiver and transcodes as needed — plays
        // any codec, incl. HEVC. Track switching restarts the stream (SetCastAudio/SubtitleTrack).
        var receiver = _castReceivers[index];
        var ip = receiver.DeviceUri?.Host;
        if (string.IsNullOrEmpty(ip)) { StatusText = "Can't cast to that device."; return; }

        if (_isCurrentVod) _resumeToMs = _player.TimeMs;
        ShowConnectionOverlay = true; ShowBufferingOverlay = true; ConnectionState = "Casting…";
        _player.CastToIp(ip, receiver.Name);
        _castMode = CastMode.Libvlc;
        IsCasting = true;
        CastTargetName = receiver.Name;
        BeginCastWindow();
        LogService.Info("Cast started", new { device = receiver.Name, ip });
    }

    /// <summary>A cast (re)start restarts the stream, so give it a fresh watchdog/confirmation window —
    /// otherwise it inherits the already-elapsed local clock and gets killed mid-startup.</summary>
    private void BeginCastWindow()
    {
        _awaitingPlayback = true;
        _awaitingSince = DateTime.UtcNow;
        _playbackRequested = true;
        _playbackConfirmed = false;
        _playStartFrames = _player?.FramesDecoded ?? 0;
        if (IsCasting) { ShowConnectionOverlay = true; ConnectionState = "Casting…"; }
    }

    [RelayCommand]
    private void StopCasting()
    {
        if (_player is null) return;
        if (_isCurrentVod && _currentPlayingUrl is not null) _resumeToMs = _player.TimeMs;
        _player.StopCasting();   // drops the chromecast sout and restarts local playback
        _castMode = CastMode.None;
        IsCasting = false;
        CastTargetName = null;
    }

    /// <summary>Ends casting without restarting; call from every full-stop path so the overlay can't stick.</summary>
    private void EndCasting()
    {
        if (!IsCasting) return;
        _player?.ClearRenderer();
        _castMode = CastMode.None;
        IsCasting = false;
        CastTargetName = null;
    }

    partial void OnVolumeChanged(int value) => _player?.SetVolume(value);

    public void LoadLastSession()
    {
        var s = _settings.Load();
        _settingsData = s;
        _updateService.IncludePrerelease = s.IncludePrerelease;
        Volume = s.DefaultVolume;
        MovieDetail.SetOmdbKey(s.OmdbApiKey);
        VodBrowser.SetOmdbKey(s.OmdbApiKey);
        SeriesBrowser.SetOmdbKey(s.OmdbApiKey);
        if (!s.AutoLoadLastSession) return;
        if (s.LastSourceType == "url" && !string.IsNullOrEmpty(s.LastSourceUrl))
            _ = LoadM3UFromUrlAsync(s.LastSourceUrl);
        else if (s.LastSourceType == "file" && !string.IsNullOrEmpty(s.LastSourceFile))
            _ = LoadM3UFromFileAsync(s.LastSourceFile);
        else if (s.LastSourceType == "xtream" && s.LastXtream is not null)
            _ = LoginXtreamAsync(new XtreamConnectionInfo
            {
                ServerUrl = s.LastXtream.ServerUrl,
                Username = s.LastXtream.Username,
                Password = s.LastXtream.Password
            });
    }

    /// <summary>
    /// Persists the current VOD's playback position so it can be resumed next time.
    /// Call before switching streams or on exit. Near-finished titles are dropped so
    /// they restart instead of resuming at the credits.
    /// ponytail: only saved on switch/close, not continuously — a crash mid-play loses
    /// the latest position. Add a periodic flush if that matters.
    /// </summary>
    public void SaveVodProgress()
    {
        if (!_isCurrentVod || string.IsNullOrEmpty(_currentPlayingUrl)) return;
        double pos = VodPosition, dur = VodDuration;
        if (dur <= 1 || pos <= 0) return;
        var url = _currentPlayingUrl;
        _settingsData = MergeAndSave(s =>
        {
            if (pos >= dur * 0.95)
                s.VodProgress.Remove(url);
            else
                s.VodProgress[url] = new VodProgressEntry
                {
                    PositionMs = (long)pos, DurationMs = (long)dur, UpdatedAt = DateTime.UtcNow
                };
            // ponytail: cap history at 200, evict oldest. Raise if users want longer memory.
            if (s.VodProgress.Count > 200)
                foreach (var k in s.VodProgress.OrderBy(e => e.Value.UpdatedAt)
                             .Take(s.VodProgress.Count - 200).Select(e => e.Key).ToList())
                    s.VodProgress.Remove(k);
        });
    }

    public void SaveWindowSize(double width, double height) =>
        _settingsData = MergeAndSave(s => { s.WindowWidth = width; s.WindowHeight = height; });

    private SettingsData MergeAndSave(Action<SettingsData> update)
    {
        var s = _settings.Load();
        update(s);
        _settings.Save(s);
        return s;
    }

    public async Task LoadM3UFromUrlAsync(string url)
    {
        try
        {
            LogService.Info("Loading M3U from URL", new { url });

            var cached = _cache.LoadChannels(url);
            M3UPlaylist playlist;
            if (cached is not null && cached.Count > 0)
            {
                playlist = new M3UPlaylist { Channels = cached, SourceUrl = url };
                StatusText = $"Loaded {cached.Count:N0} channels from cache";
                LogService.Info("M3U loaded from cache", new { channelCount = cached.Count });
            }
            else
            {
                StatusText = "Downloading playlist...";
                playlist = await _parser.LoadFromUrlAsync(url);
                foreach (var ch in playlist.Channels)
                    ch.Type = ChannelClassifier.Classify(ch);
                _cache.SaveChannels(url, playlist.Channels);
                LogService.Info("M3U loaded from URL and cached", new { channelCount = playlist.Channels.Count });
            }

            _pendingPlaylist = playlist;
            _allChannels = [];
            _liveChannels = [];
            _movieChannels = [];
            _seriesChannels = [];
            IsConnected = true;
            ConnectionLabel = $"{playlist.Channels.Count:N0} channels";
            MergeAndSave(s => { s.LastSourceType = "url"; s.LastSourceUrl = url; });
            await AutoLaunchAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load M3U from URL", new { url, error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
    }

    public async Task LoadM3UFromFileAsync(string path)
    {
        try
        {
            LogService.Info("Loading M3U from file", new { path });

            var key = $"file:{path}:{new FileInfo(path).LastWriteTimeUtc:O}";
            var cached = _cache.LoadChannels(key);
            M3UPlaylist playlist;
            if (cached is not null && cached.Count > 0)
            {
                playlist = new M3UPlaylist { Channels = cached, SourceFile = path };
                StatusText = $"Loaded {cached.Count:N0} channels from cache";
                LogService.Info("M3U loaded from cache", new { channelCount = cached.Count });
            }
            else
            {
                StatusText = "Loading file...";
                playlist = await _parser.LoadFromFileAsync(path);
                foreach (var ch in playlist.Channels)
                    ch.Type = ChannelClassifier.Classify(ch);
                _cache.SaveChannels(key, playlist.Channels);
                LogService.Info("M3U loaded from file and cached", new { channelCount = playlist.Channels.Count });
            }

            _pendingPlaylist = playlist;
            _allChannels = [];
            _liveChannels = [];
            _movieChannels = [];
            _seriesChannels = [];
            IsConnected = true;
            ConnectionLabel = $"{playlist.Channels.Count:N0} channels";
            MergeAndSave(s => { s.LastSourceType = "file"; s.LastSourceFile = path; });
            await AutoLaunchAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load M3U from file", new { path, error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
    }

    public async Task LoginXtreamAsync(XtreamConnectionInfo info)
    {
        try
        {
            LogService.Info("Authenticating Xtream", new { server = info.ServerUrl, user = info.Username });
            StatusText = "Authenticating...";
            await _xtream.ValidateAsync(info);
            _pendingXtreamInfo = info;
            _activeXtreamInfo = info;
            OnPropertyChanged(nameof(CanUseTimeline));

            MergeAndSave(s =>
            {
                s.LastSourceType = "xtream";
                s.LastXtream = new XtreamSettings
                {
                    ServerUrl = info.ServerUrl,
                    Username = info.Username,
                    Password = info.Password
                };
            });
            LogService.Info("Xtream authentication successful");
            await AutoLaunchAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Xtream authentication failed", new { error = ex.Message });
            StatusText = $"Xtream error: {ex.Message}";
        }
    }

    public async Task LoadXtreamLiveChannelsAsync()
    {
        if (_pendingXtreamInfo is null) return;
        try
        {
            StatusText = "Loading channels...";
            var channels = await _xtream.GetLiveStreamsAsync(_pendingXtreamInfo);
            _allChannels = channels.Select(ch => new ChannelListItemViewModel(ch)).ToList();
            _liveChannels = _allChannels.Where(c => c.Type == ChannelType.LiveTv).ToList();
            _movieChannels = _allChannels.Where(c => c.Type == ChannelType.Movie).ToList();
            _seriesChannels = _allChannels.Where(c => c.Type == ChannelType.Series).ToList();
            ChannelCount = _allChannels.Count;
            RestoreFavorites();
            BuildCategories();
            RestoreLastSessionSelection();
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to load Xtream channels", new { error = ex.Message });
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GoBackToPicker()
    {
        LogService.Info("GoBackToPicker called", new { mode = Mode.ToString(), isPlaying = IsPlaying, isPaused = IsPaused });
        Mode = ContentMode.Welcome;
        IsBrowsing = false;
        FilteredChannels.Clear();
        Categories.Clear();
        SelectedChannel = null;
        _player?.Stop();
        IsPlaying = false;
        IsPaused = false;
        StatusText = "Ready";
    }

    [RelayCommand]
    private void GoBack()
    {
        if (IsPlaying || IsPaused)
        {
            StopPlayback();
        }
        else if (Mode == ContentMode.MovieDetail)
        {
            Mode = _detailReturnMode;
        }
        else
        {
            Mode = ContentMode.Welcome;
            IsBrowsing = false;
            FilteredChannels.Clear();
            Categories.Clear();
            SelectedChannel = null;
            _player?.Stop();
            IsPlaying = false;
            IsPaused = false;
            StatusText = "Ready";
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        LogService.Info("StopPlayback called", new { mode = Mode.ToString(), isPlaying = IsPlaying, isPaused = IsPaused });
        SaveVodProgress();           // persist position before we zero it below
        _player?.Stop();
        EndCasting();
        IsPlaying = false;
        IsPaused = false;
        SelectedChannel = null;
        VodPosition = 0;
        VodDuration = 1;
        ShowBufferingOverlay = false;
        ShowConnectionOverlay = false;
        _awaitingPlayback = false;
        _playbackRequested = false;
        _playbackConfirmed = false;
        _player?.ClearBitmap();

        if (Mode == ContentMode.LiveTv)
        {
            IsBrowsing = true;
            ApplyFilters();   // grid was hidden through the play/cast episode; repopulate so it isn't empty
            StatusText = "Browsing channels";
        }
        else
        {
            StatusText = "Ready";
            if (Mode == ContentMode.Movies)
                VodBrowser.RefreshContinueWatching(_settingsData.VodProgress);
        }
    }

    [RelayCommand]
    private void SelectChannel(ChannelListItemViewModel? channel)
    {
        if (channel is not null)
            SelectedChannel = channel;
    }

    public async Task ShowPlaylistContentAsync()
    {
        if (_allChannels.Count > 0) return;

        if (_pendingPlaylist is null)
        {
            if (_pendingXtreamInfo is not null)
                await LoadXtreamLiveChannelsAsync();
            return;
        }

        var playlist = _pendingPlaylist;
        foreach (var ch in playlist.Channels)
            ch.Type = ChannelClassifier.Classify(ch);

        _allChannels = playlist.Channels.Select(ch => new ChannelListItemViewModel(ch)).ToList();
        _liveChannels = _allChannels.Where(c => c.Type == ChannelType.LiveTv).ToList();
        _movieChannels = _allChannels.Where(c => c.Type == ChannelType.Movie).ToList();
        _seriesChannels = _allChannels.Where(c => c.Type == ChannelType.Series).ToList();
        ChannelCount = _allChannels.Count;
        RestoreFavorites();
        LogService.Info($"Classified {_allChannels.Count}: {_liveChannels.Count} live, {_movieChannels.Count} movies, {_seriesChannels.Count} series");
    }

    [ObservableProperty]
    private string _connectionLabel = "Not connected";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionHost))]
    private bool _isConnected;

    /// <summary>Title-bar connection cluster text: "Connected · {host}" or a disconnected hint.</summary>
    public string ConnectionHost
    {
        get
        {
            if (!IsConnected) return "No source connected";
            var url = ConnectionServerUrl;
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return $"Connected · {uri.Host}";
            return "Connected";
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (_player is null) return;
        try
        {
            LogService.Info("TogglePlayPause entered", new { mode = Mode.ToString(), isPlaying = IsPlaying, isPaused = IsPaused });
            if (IsPlaying)
            {
                LogService.Info("TogglePlayPause: pause");
                _player.SetPause(true);
                IsPaused = true;
                IsPlaying = false;
                StatusText = "Paused";
            }
            else if (IsPaused)
            {
                LogService.Info("TogglePlayPause: resume");
                _player.SetPause(false);
                IsPlaying = true;
                IsPaused = false;
                StatusText = "Playing";
            }
        }
        catch (Exception ex)
        {
            LogService.Error("TogglePlayPause failed", new { error = ex.Message });
        }
    }

    [RelayCommand]
    private void Rewind() => _player?.Seek(-10000);

    [RelayCommand]
    private void Forward() => _player?.Seek(10000);

    [RelayCommand]
    private void ToggleMute()
    {
        if (_player is null) return;
        var newMuted = !IsMuted;
        _player.MuteAudio(newMuted);
        IsMuted = newMuted;
    }

    [RelayCommand]
    private void Fullscreen()
    {
        ToggleFullscreenRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleFavorite(ChannelListItemViewModel? channel)
    {
        if (channel is null) return;
        channel.IsFavorite = !channel.IsFavorite;
        SaveFavorites();
        if (ShowFavoritesOnly && !channel.IsFavorite)
            FilteredChannels.Remove(channel);
    }

    private void SaveFavorites()
    {
        var favs = _allChannels.Where(c => c.IsFavorite).Select(c => c.Url).ToList();
        var s = _settings.Load();
        s.FavoriteUrls = favs;
        _settings.Save(s);
    }


    private void RestoreFavorites()
    {
        var favs = _settings.Load().FavoriteUrls;
        if (favs.Count == 0) return;
        var set = new HashSet<string>(favs, StringComparer.OrdinalIgnoreCase);
        foreach (var ch in _allChannels)
            ch.IsFavorite = set.Contains(ch.Url);
    }

    private void BuildCategories()
    {
        Categories.Clear();
        FilteredChannels.Clear();

        var groups = _allChannels
            .GroupBy(ch => ch.Group ?? "Uncategorized")
            .OrderBy(g => g.Key == "Uncategorized" ? 1 : 0)
            .ThenBy(g => g.Key);

        foreach (var g in groups)
        {
            var cat = new CategoryViewModel { Name = g.Key };
            cat.Channels.AddRange(g);
            Categories.Add(cat);
        }
    }

    private void SelectFirstCategory()
    {
        if (Categories.Count > 0)
            SelectedCategory = Categories[0];
    }

    /// <summary>Startup auto-check: runs the same check but stays silent on the no-update / not-supported
    /// paths (only an available update surfaces, via the UpdateReady dialog). The toast is manual-only. (#94)</summary>
    public Task CheckForUpdatesSilentAsync() => _updateService.CheckAndDownloadAsync();

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        var progress = new Progress<double>(p => UpdateDownloadProgress = p);
        var result = await _updateService.CheckAndDownloadAsync(progress);
        UpdateDownloadProgress = 0;

        // Manual check: always give feedback. (UpdateReady already opens the update dialog.) (#94)
        switch (result)
        {
            case UpdateCheckResult.UpToDate:
                ShowToastMessage($"You're on the newest version ✅  (v{_updateService.CurrentVersion})");
                break;
            case UpdateCheckResult.NotSupported:
                ShowToastMessage("Updates aren't available for this build.");
                break;
            case UpdateCheckResult.Failed:
                ShowToastMessage("Update check failed — try again later.");
                break;
        }
    }

    [ObservableProperty] private string? _toastMessage;
    [ObservableProperty] private bool _showToast;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToastCanCopy))]
    private string? _toastCopyText;
    private int _toastToken;

    /// <summary>When set, clicking the toast copies this text (e.g. a recording path). (#84)</summary>
    public bool ToastCanCopy => !string.IsNullOrEmpty(ToastCopyText);

    /// <summary>Show a small transient toast that auto-hides. Pass <paramref name="copyText"/> to make
    /// it clickable-to-copy. (#94, #84)</summary>
    public async void ShowToastMessage(string message, string? copyText = null)
    {
        ToastMessage = message;
        ToastCopyText = copyText;
        ShowToast = true;
        var token = ++_toastToken;
        await Task.Delay(copyText is null ? 4000 : 7000);
        if (token == _toastToken)
            ShowToast = false;
    }

    // Recording taps the main player's single connection (see PlaybackService.StartRecording). Live only:
    // sout muxes VOD faster than real-time, so on-demand recording is out of scope here. (#84)
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private string _recordingElapsed = "0:00";
    private DispatcherTimer? _recordTimer;
    private DateTime _recordStarted;
    private string? _recordPath;

    public static string RecordingsFolder =>
        System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos), "Sabeltann");

    [RelayCommand]
    private void ToggleRecord()
    {
        if (_player is null || string.IsNullOrEmpty(_currentPlayingUrl))
        {
            ShowToastMessage("Start playing a live channel first, then record.");
            return;
        }
        if (IsRecording)
        {
            _player.StopRecording();
            IsRecording = false;
            _recordTimer?.Stop();
            ShowToastMessage("Recording saved — click to copy path", copyText: _recordPath);
            return;
        }
        if (_isCurrentVod)
        {
            ShowToastMessage("Recording is for live TV, not on-demand.");
            return;
        }
        try
        {
            System.IO.Directory.CreateDirectory(RecordingsFolder);
            _recordPath = System.IO.Path.Combine(RecordingsFolder, $"Sabeltann_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ts");
            _player.StartRecording(_recordPath);
            IsRecording = true;
            _recordStarted = DateTime.UtcNow;
            RecordingElapsed = "0:00";
            _recordTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _recordTimer.Tick -= OnRecordTick;
            _recordTimer.Tick += OnRecordTick;
            _recordTimer.Start();
        }
        catch (Exception ex)
        {
            ShowToastMessage("Couldn't start recording.");
            LogService.Error("Recording start failed", new { error = ex.Message });
        }
    }

    private void OnRecordTick(object? sender, EventArgs e)
    {
        var t = DateTime.UtcNow - _recordStarted;
        RecordingElapsed = $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
    }

    private void ShowUpdateDialog(string newVersion)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            var vm = new UpdateDialogViewModel
            {
                CurrentVersion = _updateService.CurrentVersion ?? "current",
                NewVersion = newVersion,
                ReleaseNotes = _updateService.ReleaseNotes,
                HasReleaseNotes = _updateService.ReleaseNotes is { Length: > 0 }
            };
            vm.CloseRequested += () =>
            {
                // Schedule the apply-on-exit with restart, then actually exit so Velopack applies and
                // relaunches. Without the shutdown the app just keeps running and nothing happens. (#80)
                if (vm.Result == UpdateDialogResult.InstallAndRestart)
                {
                    _updateService.ApplyPendingOnExit(restart: true);
                    if (Avalonia.Application.Current?.ApplicationLifetime is
                        Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d)
                        d.Shutdown();
                }
            };
            var dialog = new UpdateDialog { DataContext = vm };
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is { } mainWin)
            {
                await dialog.ShowDialog(mainWin);
            }
        });
    }

    public UpdateService GetUpdateService() => _updateService;

    private void RestoreLastSessionSelection()
    {
        var s = _settings.Load();

        if (!string.IsNullOrEmpty(s.LastCategoryName))
        {
            var cat = Categories.FirstOrDefault(c =>
                c.Name.Equals(s.LastCategoryName, StringComparison.OrdinalIgnoreCase));
            if (cat is not null)
                SelectedCategory = cat;
            else if (Categories.Count > 0)
                SelectedCategory = Categories[0];
        }
        else if (Categories.Count > 0)
        {
            SelectedCategory = Categories[0];
        }

        if (!string.IsNullOrEmpty(s.LastChannelUrl) && SelectedCategory is not null)
        {
            var ch = SelectedCategory.Channels.FirstOrDefault(c =>
                c.Url.Equals(s.LastChannelUrl, StringComparison.OrdinalIgnoreCase))
                ?? _allChannels.FirstOrDefault(c =>
                    c.Url.Equals(s.LastChannelUrl, StringComparison.OrdinalIgnoreCase));
            if (ch is not null)
                SelectedChannel = ch;
        }
    }
}

public class SubtitleTrackItem(int id, string name)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public bool IsOff => Id == -1;
}
