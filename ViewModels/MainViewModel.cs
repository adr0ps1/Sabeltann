using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly M3UParser _parser = new();
    private readonly XtreamService _xtream = new();
    private readonly SettingsService _settings = new();
    private PlaybackService? _player;
    private List<ChannelListItemViewModel> _allChannels = [];
    private List<ShowGroup> _allShowGroups = [];
    private List<ChannelListItemViewModel> _liveChannels = [];
    private List<ChannelListItemViewModel> _vodChannels = [];

    [ObservableProperty]
    private ObservableCollection<CategoryViewModel> _categories = [];

    [ObservableProperty]
    private ObservableCollection<ShowGroup> _showGroups = [];

    [ObservableProperty]
    private ObservableCollection<ChannelListItemViewModel> _filteredChannels = [];

    [ObservableProperty]
    private CategoryViewModel? _selectedCategory;

    [ObservableProperty]
    private ChannelListItemViewModel? _selectedChannel;

    [ObservableProperty]
    private ShowGroup? _selectedGroup;

    partial void OnSelectedGroupChanged(ShowGroup? value)
    {
        if (value is null) return;
        FilteredChannels.Clear();
        foreach (var ch in value.Channels)
            FilteredChannels.Add(ch);
        if (FilteredChannels.Count > 0)
            SelectedChannel = FilteredChannels[0];
        StatusText = $"{value.Name} — {value.Count} episodes";
    }

    public void SetupShowGroups()
    {
        ShowGroups.Clear();
        foreach (var g in _allShowGroups)
            ShowGroups.Add(g);
    }

    private void ApplyChannelSplit()
    {
        var split = ChannelGrouper.SplitByType(_allChannels);
        _liveChannels = split.Live;
        _vodChannels = split.Vod;
    }

    public void ShowLiveChannels()
    {
        FilteredChannels.Clear();
        Categories.Clear();
        ShowContentPicker = false;
        _allChannels = [.. _liveChannels];
        ChannelCount = _allChannels.Count;
        HasContent = ChannelCount > 0;
        RestoreFavorites();
        BuildCategories();
    }

    public void ShowVodChannels()
    {
        FilteredChannels.Clear();
        Categories.Clear();
        ShowContentPicker = false;
        _allChannels = [.. _vodChannels];
        ChannelCount = _allChannels.Count;
        HasContent = ChannelCount > 0;
        RestoreFavorites();
        BuildCategories();
        SetupShowGroups();
    }

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _volume = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseSymbol))]
    private bool _isPlaying;

    public string PlayPauseSymbol => IsPlaying ? "⏸" : "⏵";

    [ObservableProperty]
    private int _channelCount;

    [ObservableProperty]
    private List<(int Id, string Name)> _subtitleTracks = [];

    [ObservableProperty]
    private int _currentSubtitleTrack = -1;

    [ObservableProperty]
    private bool _showFavoritesOnly;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowWelcome))]
    private bool _hasContent;

    public bool ShowWelcome => !HasContent;

    [ObservableProperty]
    private string _connectionState = "";

    [ObservableProperty]
    private int _connectionProgress;

    [ObservableProperty]
    private bool _showConnectionOverlay;

    [ObservableProperty]
    private bool _showContentPicker;

    [ObservableProperty]
    private bool _showCategoryGrid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFlatList))]
    private bool _showGroupsList;

    public bool ShowFlatList => !ShowGroupsList;

    private XtreamConnectionInfo? _pendingXtreamInfo;
    private M3UPlaylist? _pendingPlaylist;

    public event Action? ToggleFullscreenRequested;

    [ObservableProperty]
    private bool _showDebugOverlay;

    public DebugStatsViewModel DebugStats { get; }

    public void SetPlayer(PlaybackService player)
    {
        _player = player;
        _player.Error += (_, msg) =>
        {
            StatusText = $"Playback error: {msg}";
            ConnectionState = msg;
            ConnectionProgress = 0;
        };
        _player.Buffering += (_, pct) =>
        {
            ConnectionProgress = pct;
            if (pct < 100)
                ConnectionState = $"Buffering... {pct}%";
            else
                ConnectionState = "Starting playback...";
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
        DebugStats.SetPlayer(player);
    }

    public MainViewModel()
    {
        DebugStats = new DebugStatsViewModel(null);
    }

    partial void OnSelectedCategoryChanged(CategoryViewModel? value)
    {
        if (value is null) return;
        ApplyFilters();
        MergeAndSave(s => s.LastCategoryName = value.Name);
    }

    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var category = SelectedCategory;
        if (category is null) return;

        var query = (SearchText ?? "").Trim().ToLowerInvariant();
        FilteredChannels.Clear();

        foreach (var ch in category.Channels)
        {
            if (ShowFavoritesOnly && !ch.IsFavorite) continue;
            if (query.Length > 0 && !ch.Name.ToLowerInvariant().Contains(query)) continue;
            FilteredChannels.Add(ch);
        }
    }

    partial void OnSelectedChannelChanged(ChannelListItemViewModel? value)
    {
        if (value is not null && _player is not null && !string.IsNullOrEmpty(value.Url))
        {
            ShowConnectionOverlay = true;
            ConnectionState = "Connecting...";
            ConnectionProgress = 0;
            _player.Play(value.Url);
            DebugStats.SetUrl(value.Url);
            IsPlaying = true;
            SubtitleTracks = _player.GetSubtitleTracks();
            CurrentSubtitleTrack = _player.CurrentSubtitleTrack;
            StatusText = $"Playing: {value.Name}";

            MergeAndSave(s => s.LastChannelUrl = value.Url);
        }
    }

    [RelayCommand]
    private void ToggleSubtitles()
    {
        if (_player is null) return;
        var next = _player.CurrentSubtitleTrack <= 0 && _player.GetSubtitleTracks().Count > 0
            ? _player.GetSubtitleTracks()[0].Id
            : -1;
        _player.SetSubtitleTrack(next);
        CurrentSubtitleTrack = next;
    }

    partial void OnVolumeChanged(int value) => _player?.SetVolume(value);

    public void LoadLastSession()
    {
        var s = _settings.Load();
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
            StatusText = "Downloading playlist...";
            var playlist = await _parser.LoadFromUrlAsync(url);
            _pendingPlaylist = playlist;
            ShowContentPicker = true;
            MergeAndSave(s => { s.LastSourceType = "url"; s.LastSourceUrl = url; });
            LogService.Info("M3U loaded from URL", new { channelCount = playlist.Channels.Count });
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
            StatusText = "Loading file...";
            var playlist = await _parser.LoadFromFileAsync(path);
            _pendingPlaylist = playlist;
            ShowContentPicker = true;
            MergeAndSave(s => { s.LastSourceType = "file"; s.LastSourceFile = path; });
            LogService.Info("M3U loaded from file", new { channelCount = playlist.Channels.Count });
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
            ShowContentPicker = true;

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
            ShowContentPicker = false;
            var channels = await _xtream.GetLiveStreamsAsync(_pendingXtreamInfo);
            _allChannels = channels.Select(ch => new ChannelListItemViewModel(ch)).ToList();
            ApplyChannelSplit();
            _allShowGroups = ChannelGrouper.GroupChannels(_allChannels);
            ChannelCount = _allChannels.Count;
            HasContent = ChannelCount > 0;
            RestoreFavorites();
            BuildCategories();
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
        HasContent = false;
        ShowContentPicker = true;
        FilteredChannels.Clear();
        Categories.Clear();
        SelectedChannel = null;
        _player?.Stop();
        IsPlaying = false;
        StatusText = "Ready";
    }

    [RelayCommand]
    private void SelectChannel(ChannelListItemViewModel? channel)
    {
        if (channel is not null)
            SelectedChannel = channel;
    }

    public async Task ShowPlaylistContentAsync()
    {
        if (_pendingPlaylist is null)
        {
            if (_pendingXtreamInfo is not null)
                await LoadXtreamLiveChannelsAsync();
            return;
        }

        ShowContentPicker = false;
        var playlist = _pendingPlaylist;
        _allChannels = playlist.Channels.Select(ch => new ChannelListItemViewModel(ch)).ToList();
        ApplyChannelSplit();
        ChannelCount = _allChannels.Count;
        HasContent = ChannelCount > 0;
        RestoreFavorites();
        BuildCategories();
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (_player is null) return;
        if (_player.IsPlaying)
        {
            _player.Pause();
            IsPlaying = false;
            StatusText = "Paused";
        }
        else if (SelectedChannel is not null)
        {
            _player.Play(SelectedChannel.Url);
            IsPlaying = true;
            StatusText = $"Playing: {SelectedChannel.Name}";
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
        _player?.Stop();
        IsPlaying = false;
        StatusText = "Stopped";
    }

    private void LoadChannels(M3UPlaylist playlist)
    {
        _allChannels = playlist.Channels.Select(ch => new ChannelListItemViewModel(ch)).ToList();
        ApplyChannelSplit();
        _allShowGroups = ChannelGrouper.GroupChannels(_allChannels);
        ChannelCount = _allChannels.Count;
        HasContent = ChannelCount > 0;
        RestoreFavorites();
        BuildCategories();
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

        RestoreLastSessionSelection();
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
