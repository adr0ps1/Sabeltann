using System.Collections.ObjectModel;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public enum ContentMode
{
    Welcome,
    Picker,
    LiveTv,
    Movies,
    Series
}

public partial class MainViewModel : ObservableObject
{
    public static readonly FuncValueConverter<bool, string> MuteIcon = new(
        muted => muted ? "🔇" : "🔊");

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
    private ChannelListItemViewModel? _selectedChannel;

    public string ConnectionServerUrl => _pendingXtreamInfo?.ServerUrl ?? _settings.Load().LastXtream?.ServerUrl ?? "";
    public string ConnectionUsername => _pendingXtreamInfo?.Username ?? _settings.Load().LastXtream?.Username ?? "";

    public SettingsData GetSettings() => _settings.Load();

    public void ApplySettings(SettingsData data)
    {
        _settings.Save(data);
        Volume = data.DefaultVolume;
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
        Mode = ContentMode.Movies;

        if (_pendingXtreamInfo is not null)
        {
            await VodBrowser.InitializeAsync(_pendingXtreamInfo);
        }
        else
        {
            VodBrowser.InitializeFromChannels(_movieChannels);
        }
    }

    public async Task ShowSeriesBrowserAsync()
    {
        SeriesBrowser.PlayRequested -= OnVodPlayRequested;
        SeriesBrowser.PlayRequested += OnVodPlayRequested;
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

    private void OnVodPlayRequested(string url)
    {
        try
        {
            _currentPlayingUrl = url;
            _vodPausePosition = TimeSpan.Zero;
            LogService.Info("Playing VOD", new { url });
            ShowConnectionOverlay = true;
            ConnectionState = "Connecting...";
            ConnectionProgress = 0;
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
    private double _vodPosition;

    [ObservableProperty]
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
    [NotifyPropertyChangedFor(nameof(PlayPauseSymbol))]
    [NotifyPropertyChangedFor(nameof(ShowOverlay))]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(ShowVideo))]
    private bool _isPlaying;

    public string PlayPauseSymbol => IsPlaying ? "⏸" : "⏵";
    public bool ShowOverlay => IsPlaying;

    public bool ShowVideo => IsPlaying || IsPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowVideo))]
    private bool _isPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome))]
    [NotifyPropertyChangedFor(nameof(IsPicker))]
    [NotifyPropertyChangedFor(nameof(IsLiveTv))]
    [NotifyPropertyChangedFor(nameof(IsMovies))]
    [NotifyPropertyChangedFor(nameof(IsSeries))]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    [NotifyPropertyChangedFor(nameof(IsCategoryBarVisible))]
    private ContentMode _mode = ContentMode.Welcome;

    partial void OnModeChanged(ContentMode value)
    {
        // The category dropdown and search only apply to Live TV mode.
        // Movies/Series have their own dedicated browsers.
    }

    public bool IsWelcome => Mode == ContentMode.Welcome;
    public bool IsPicker => Mode == ContentMode.Picker;
    public bool IsLiveTv => Mode == ContentMode.LiveTv;
    public bool IsMovies => Mode == ContentMode.Movies;
    public bool IsSeries => Mode == ContentMode.Series;
    public bool ShowChannelGrid => Mode == ContentMode.LiveTv && IsBrowsing;
    public bool IsCategoryBarVisible => Mode == ContentMode.LiveTv;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowChannelGrid))]
    private bool _isBrowsing;

    [ObservableProperty]
    private int _channelCount;

    [ObservableProperty]
    private bool _showSubtitlePopup;

    public ObservableCollection<SubtitleTrackItem> SubtitleTrackItems { get; } = [];

    [ObservableProperty]
    private int _currentSubtitleTrack = -1;

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

    public VodBrowserViewModel VodBrowser { get; } = new();
    public SeriesBrowserViewModel SeriesBrowser { get; } = new();

    private XtreamConnectionInfo? _pendingXtreamInfo;
    private M3UPlaylist? _pendingPlaylist;
    private string? _currentPlayingUrl;
    private TimeSpan _vodPausePosition;

    public event Action? ToggleFullscreenRequested;

    [ObservableProperty]
    private bool _showDebugOverlay;

    public WriteableBitmap? VideoBitmap => _player?.VideoBitmap;

    public DebugStatsViewModel DebugStats { get; }

    public void SetPlayer(PlaybackService player)
    {
        _player = player;
        _player.Error += (_, msg) =>
        {
            LogService.Info("VLC Error event fired", new { msg, isPlaying = IsPlaying, isPaused = IsPaused });
            StatusText = $"Playback error: {msg}";
            ConnectionState = msg;
            ConnectionProgress = 0;
            IsPlaying = false;
            IsPaused = false;
            ShowConnectionOverlay = false;
        };
        _player.PlayingStarted += (_, _) =>
        {
            ConnectionState = "";
            ShowConnectionOverlay = false;
            ConnectionProgress = 0;
        };
        _player.Stopped += (_, _) =>
        {
            ConnectionState = "";
            ShowConnectionOverlay = false;
            ConnectionProgress = 0;
        };
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += (_, _) =>
        {
            if (_player is not null && (IsPlaying || IsPaused))
            {
                var len = _player.Length;
                if (len > 0) VodDuration = len;
                VodPosition = _player.TimeMs;
            }

            if (_player?.VideoBitmap is not null)
            {
                OnPropertyChanged(nameof(VideoBitmap));
            }
        };
        _positionTimer.Start();

        player.PlayingStarted += (_, _) =>
        {
            var len = _player.Length;
            if (len > 0) VodDuration = len;
        };
        DebugStats.SetPlayer(player, this);
    }

    public MainViewModel()
    {
        DebugStats = new DebugStatsViewModel(null);
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
                _currentPlayingUrl = value.Url;
                _vodPausePosition = TimeSpan.Zero;
                _player.SetVolume(Volume);
                LogService.Info("Playing channel", new { name = value.Name, url = value.Url });
                ShowConnectionOverlay = true;
                ConnectionState = "Connecting...";
                ConnectionProgress = 0;
                _player.Play(value.Url);
                DebugStats.SetUrl(value.Url);
                IsPlaying = true;
                IsPaused = false;
                SubtitleTrackItems.Clear();
                SubtitleTrackItems.Add(new SubtitleTrackItem(-1, "Off"));
                foreach (var t in _player.GetSubtitleTracks())
                    SubtitleTrackItems.Add(new SubtitleTrackItem(t.Id, t.Name));
                CurrentSubtitleTrack = _player.CurrentSubtitleTrack;
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
        _player.SetSubtitleTrack(id);
        CurrentSubtitleTrack = id;
        ShowSubtitlePopup = false;
    }

    partial void OnVolumeChanged(int value) => _player?.SetVolume(value);

    public void LoadLastSession()
    {
        var s = _settings.Load();
        Volume = s.DefaultVolume;
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
            Mode = ContentMode.Picker;
            MergeAndSave(s => { s.LastSourceType = "url"; s.LastSourceUrl = url; });
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
            Mode = ContentMode.Picker;
            MergeAndSave(s => { s.LastSourceType = "file"; s.LastSourceFile = path; });
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
            Mode = ContentMode.Picker;

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
        Mode = ContentMode.Picker;
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
    private void StopPlayback()
    {
        LogService.Info("StopPlayback called", new { mode = Mode.ToString(), isPlaying = IsPlaying, isPaused = IsPaused });
        _player?.Stop();
        IsPlaying = false;
        IsPaused = false;
        SelectedChannel = null;
        VodPosition = 0;
        VodDuration = 1;

        if (Mode == ContentMode.LiveTv)
        {
            IsBrowsing = true;
            StatusText = "Browsing channels";
        }
        else
        {
            StatusText = "Ready";
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
    private bool _isConnected;

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
    private void Rewind()
    {
        _player?.Seek(-10000);
    }

    [RelayCommand]
    private void Forward()
    {
        _player?.Seek(10000);
    }

    [RelayCommand]
    private void ToggleMute()
    {
        _player?.ToggleMute();
        IsMuted = _player?.IsMuted ?? false;
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

    [RelayCommand]
    private void Stop()
    {
        LogService.Info("Stop command called", new { isPlaying = IsPlaying, isPaused = IsPaused });
        _player?.Stop();
        _player?.SetVolume(Volume);
        _vodPausePosition = TimeSpan.Zero;
        IsPlaying = false;
        IsPaused = false;
        VodPosition = 0;
        VodDuration = 1;
        StatusText = "Stopped";
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
