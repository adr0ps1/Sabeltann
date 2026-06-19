using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Sabeltann.Models;
using Sabeltann.Services;
using Sabeltann.ViewModels;

namespace Sabeltann;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly PlaybackService _player;
    private readonly UpdateService _updates = new();
    private readonly DispatcherTimer _transportTimer;
    private bool _isFullscreen;
    private bool _updateRestartPending;

    public MainWindow()
    {
        InitializeComponent();
        LogService.Info("Application started");
        _player = new PlaybackService();
        _vm = new MainViewModel();
        _vm.SetPlayer(_player);
        _vm.ToggleFullscreenRequested += ToggleFullscreen;
        DataContext = _vm;

        if (_player?.Player is not null)
            VideoView.Attach(_player.Player);
        else
            LogService.Warn("VLC player not available");
        VideoView.MouseActivity += ShowTransport;
        KeyDown += OnKeyDown;

        _transportTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _transportTimer.Tick += (_, _) => { TransportPopup.IsOpen = false; _transportTimer.Stop(); };

        void ShowTransport()
        {
            if (_vm.IsPlaying && IsActive)
            {
                TransportPopup.IsOpen = true;
                _transportTimer.Stop();
                _transportTimer.Start();
            }
        }

        PointerMoved += (_, _) => ShowTransport();

        LoadPirateIcon();
        _vm.LoadLastSession();

        TransportPopup.PlacementTarget = VideoView;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsPlaying) || e.PropertyName == nameof(MainViewModel.IsPaused))
            {
                if (_vm.IsPlaying)
                {
                    TransportPopup.IsOpen = true;
                    _transportTimer.Stop();
                    _transportTimer.Start();
                }
                else
                {
                    TransportPopup.IsOpen = false;
                    OverlayPanel.Opacity = 0;
                }
                if (!_vm.IsPlaying && !_vm.IsPaused && _isFullscreen)
                    ToggleFullscreen();
            }
        };

        Opened += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
        };

        _updates.UpdateReady += async version =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                _vm.StatusText = $"Update {version} available";
                var dialog = new UpdateDialog(version);
                await dialog.ShowDialog(this);
                if (dialog.RestartRequested)
                {
                    _updateRestartPending = true;
                    _updates.ApplyPendingOnExit(restart: true);
                    Close();
                }
            });
        };
        Opened += (_, _) => _ = _updates.CheckAndDownloadAsync();

        ConnectionPage.LoadM3UFileRequested += OnLoadM3UFile;
        ConnectionPage.LoadM3UUrlRequested += OnLoadM3UUrl;
        ConnectionPage.XtreamLoginRequested += OnXtreamLogin;

        ContentPicker.LiveTvSelected += async (_, _) =>
        {
            await _vm.ShowPlaylistContentAsync();
            _vm.ShowLiveChannels();
        };
        ContentPicker.MoviesSelected += async (_, _) =>
        {
            await _vm.ShowPlaylistContentAsync();
            await _vm.ShowMoviesBrowserAsync();
        };
        ContentPicker.SeriesSelected += async (_, _) =>
        {
            await _vm.ShowPlaylistContentAsync();
            await _vm.ShowSeriesBrowserAsync();
        };

    }

    private void LoadPirateIcon()
    {
        try
        {
            using var stream = typeof(MainWindow).Assembly
                .GetManifestResourceStream("Sabeltann.Assets.Sabeltann.ico");
            if (stream is null) return;
            Icon = new WindowIcon(stream);
        }
        catch { }
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = WindowState != WindowState.FullScreen;
        WindowState = _isFullscreen ? WindowState.FullScreen : WindowState.Normal;

        TitleBar.IsVisible = !_isFullscreen;
        MainMenu.IsVisible = !_isFullscreen;
        StatusBar.IsVisible = !_isFullscreen;

        if (_isFullscreen)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(0);
            MainGrid.RowDefinitions[1].Height = new GridLength(0);
            MainGrid.RowDefinitions[3].Height = new GridLength(0);
        }
        else
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(44);
            MainGrid.RowDefinitions[1].Height = GridLength.Auto;
            MainGrid.RowDefinitions[3].Height = new GridLength(28);
        }

        TransportPopup.PlacementTarget = VideoView;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // ESC during playback: stop and return to browsing, don't go all the way back
        if (e.Key == Key.Escape && (_vm.IsPlaying || _vm.IsPaused))
        {
            _vm.StopPlaybackCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.Mode == ContentMode.Picker)
        {
            _vm.Mode = ContentMode.Welcome;
            _vm.StatusText = "Ready";
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.Mode == ContentMode.LiveTv)
        {
            _vm.GoBackToPickerCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && (_vm.Mode == ContentMode.Movies || _vm.Mode == ContentMode.Series))
        {
            _vm.GoBackToPickerCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            _vm.TogglePlayPauseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.D)
        {
            _vm.ShowDebugOverlay = !_vm.ShowDebugOverlay;
                DebugPopup.IsOpen = _vm.ShowDebugOverlay;
            e.Handled = true;
        }
    }

    private void OnOverlayEntered(object? sender, PointerEventArgs e)
    {
        OverlayPanel.Opacity = 1;
    }

    private void OnOverlayExited(object? sender, PointerEventArgs e)
    {
        OverlayPanel.Opacity = 0;
    }

    private async void OnLoadM3UUrl(object? sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("Enter M3U URL:", "");
        var result = await dialog.ShowDialog<string?>(this);
        if (result is not null)
            await _vm.LoadM3UFromUrlAsync(result);
    }

    private async void OnLoadM3UFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select M3U Playlist",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("M3U Playlist") { Patterns = ["*.m3u", "*.m3u8"] }]
        });

        if (files.Count > 0)
            await _vm.LoadM3UFromFileAsync(files[0].Path.LocalPath);
    }

    private async void OnXtreamLogin(object? sender, RoutedEventArgs e)
    {
        var dialog = new LoginWindow();
        var result = await dialog.ShowDialog<XtreamConnectionInfo?>(this);
        if (result is not null)
            await _vm.LoginXtreamAsync(result);
    }

    private async void OnSettings(object? sender, RoutedEventArgs e)
    {
        var settings = _vm.GetSettings();
        var dialog = new Views.SettingsWindow(settings, _vm.ConnectionServerUrl, _vm.ConnectionUsername, _vm.ChannelCount);
        var result = await dialog.ShowDialog<SettingsData?>(this);
        if (result is not null)
            _vm.ApplySettings(result);
    }

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCcClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _vm.RefreshSubtitleTracks();
        var menu = new ContextMenu();
        void AddItem(string header, int id)
        {
            var mi = new MenuItem { Header = header, Tag = id };
            mi.Click += (_, _) =>
            {
                _vm.SelectSubtitleCommand.Execute(id);
                menu.Close();
            };
            menu.Items.Add(mi);
        }
        AddItem("Off", -1);
        foreach (var t in _vm.SubtitleTrackItems)
        {
            if (t.Id != -1)
                AddItem(t.Name, t.Id);
        }
        menu.Open(btn);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
        {
            TransportPopup.IsOpen = false;
            OverlayPanel.Opacity = 0;
            _transportTimer.Stop();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.DebugStats.Stop();
        VideoView.Detach();
        _player.Dispose();
        ImageService.Shutdown();
        if (!_updateRestartPending)
            _updates.ApplyPendingOnExit(restart: false);
        base.OnClosed(e);
    }
}

