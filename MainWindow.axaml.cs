using System.Runtime.InteropServices;
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
    private readonly CastService _cast = new();
    private readonly DispatcherTimer _transportAutoHide;
    private readonly DispatcherTimer _volumeHideTimer;
    private bool _isFullscreen;
    private WindowState _preFullscreenState = WindowState.Normal;
    private PopoutWindow? _popout;
    private bool _castMenuOpen;   // while true, the transport bar must not auto-hide
    private DateTime _castMenuClosedAt;   // debounce so a click that light-dismisses the menu doesn't reopen it

    public MainWindow()
    {
        InitializeComponent();
        LogService.Info("Application started");
        _player = new PlaybackService();
        _vm = new MainViewModel();
        _vm.SetPlayer(_player);
        // Cast discovery needs an inbound mDNS firewall rule or Windows drops the responses and no
        // devices are ever found. Add it off-thread so the one-time UAC prompt can't block startup.
        System.Threading.Tasks.Task.Run(CastService.EnsureMdnsFirewallRule);
        _vm.SetCastService(_cast);
        _player.FrameRendered += () => { VideoImage?.InvalidateVisual(); _popout?.InvalidateVideo(); };
        _vm.ToggleFullscreenRequested += ToggleFullscreen;
        _vm.PropertyChanged += OnVmPropertyChanged;
        DataContext = _vm;

        KeyDown += OnKeyDown;

        _transportAutoHide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _transportAutoHide.Tick += (_, _) => { if (_castMenuOpen || _vm.IsCasting) return; TransportBar.Opacity = 0; _transportAutoHide.Stop(); };

        // Transport bar stays visible while hovered; starts countdown when mouse leaves
        TransportBar.PointerEntered += (_, _) => _transportAutoHide.Stop();
        TransportBar.PointerExited  += (_, _) => { _transportAutoHide.Stop(); _transportAutoHide.Start(); };

        // Volume overlay: 300ms delay bridges the button→overlay gap without feeling slow
        _volumeHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _volumeHideTimer.Tick += (_, _) => { _vm.ShowVolumePopup = false; _volumeHideTimer.Stop(); };

        VideoBorder.PointerMoved += (_, _) => ShowTransport();
        VideoBorder.PointerPressed += (_, _) => { _vm.ShowVolumePopup = false; _volumeHideTimer.Stop(); };

        LoadPirateIcon();
        _vm.LoadLastSession();
        RestoreWindowSize();

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.IsPlaying) or nameof(MainViewModel.IsPaused)
                or nameof(MainViewModel.IsCasting))
            {
                if (_vm.IsPlaying || _vm.IsPaused || _vm.IsCasting)
                    ShowTransport();
                else
                    TransportBar.Opacity = 0;
            }
        };

        Opened += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
        };

        Opened += (_, _) => _ = _vm.CheckForUpdatesCommand.ExecuteAsync(null);

        Opened += (_, _) => TryRoundCorners();

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

    private void ShowTransport()
    {
        if (_vm.IsPlaying || _vm.IsPaused || _vm.IsCasting)
        {
            TransportBar.Opacity = 1;
            _transportAutoHide.Stop();
            _transportAutoHide.Start();
        }
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
        if (WindowState != WindowState.FullScreen)
        {
            _preFullscreenState = WindowState; // remember Maximized vs Normal so we can restore it
            _isFullscreen = true;
            WindowState = WindowState.FullScreen;
        }
        else
        {
            _isFullscreen = false;
            WindowState = _preFullscreenState;
        }
        UpdateChrome();
    }

    private void OnPopoutClick(object? sender, RoutedEventArgs e) => TogglePopout();

    // When playback ends, close the pop-out so the video returns inline.
    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ShowVideo) && !_vm.ShowVideo && _popout is not null)
            _popout.Close();

        // Morph the toolbar: quick fade-in whenever the active sub-bar swaps.
        if (e.PropertyName is nameof(MainViewModel.Mode) or nameof(MainViewModel.ShowVideo))
            AnimateToolbarMorph();

        // While fullscreen, starting/stopping video flips immersive chrome — re-evaluate. (#83)
        if (e.PropertyName == nameof(MainViewModel.ShowVideo) && _isFullscreen)
            UpdateChrome();
    }

    private void AnimateToolbarMorph()
    {
        ToolbarContent.Opacity = 0;
        Dispatcher.UIThread.Post(() => ToolbarContent.Opacity = 1, DispatcherPriority.Render);
    }

    /// <summary>Detach the video into a floating, always-on-top window (or return it inline).</summary>
    private void TogglePopout()
    {
        if (_popout is null)
            OpenPopout();
        else
            _popout.Close(); // Closed handler returns the video inline
    }

    private void OpenPopout()
    {
        _popout = new PopoutWindow { DataContext = _vm };
        _popout.Closed += (_, _) => { _popout = null; ApplyPopoutState(); };
        ApplyPopoutState();
        _popout.Show();
    }

    // While popped out, the inline surface is replaced by a placeholder so the main window is usable
    // (browse, other apps) without a second copy of the video competing for it.
    private void ApplyPopoutState()
    {
        var popped = _popout is not null;
        _vm.IsPoppedOut = popped;
        VideoImage.IsVisible = !popped;
        PopoutPlaceholder.IsVisible = popped;
    }

    /// <summary>In fullscreen the title bar is always hidden, but the morphing toolbar (which carries the
    /// category/filter bar) stays visible while browsing — it's only hidden for immersive video. (#83)</summary>
    private void UpdateChrome()
    {
        var hideTitle = _isFullscreen;
        var hideToolbar = _isFullscreen && _vm.ShowVideo;
        TitleBar.IsVisible = !hideTitle;
        Toolbar.IsVisible = !hideToolbar;
        MainGrid.RowDefinitions[0].Height = new GridLength(hideTitle ? 0 : 40);
        MainGrid.RowDefinitions[1].Height = new GridLength(hideToolbar ? 0 : 54);
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    // Click the center pause overlay (only visible while paused) to resume playback.
    private void OnPauseOverlayPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _vm.TogglePlayPauseCommand.Execute(null);
        e.Handled = true;
    }

    private void OnMinimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseWindow(object? sender, RoutedEventArgs e) => Close();

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // Round the frameless window's corners via DWM (Win11). No-op elsewhere.
    private void TryRoundCorners()
    {
        if (!OperatingSystem.IsWindows()) return;
        var h = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (h == IntPtr.Zero) return;
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const int DWMWCP_ROUND = 2;
        var pref = DWMWCP_ROUND;
        try { DwmSetWindowAttribute(h, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); } catch { }
    }

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.Tag is string edgeName &&
            Enum.TryParse<WindowEdge>(edgeName, out var edge) &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(edge, e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _popout is not null)
        {
            _popout.Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            // Esc in fullscreen exits fullscreen only — playback keeps going. A second
            // Esc (now windowed) falls through to the stop branch below. (#82)
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && (_vm.IsPlaying || _vm.IsPaused))
        {
            _vm.StopPlaybackCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.Mode == ContentMode.Picker && !_vm.IsConnected)
        {
            _vm.Mode = ContentMode.Welcome;
            _vm.StatusText = "Ready";
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.Mode == ContentMode.MovieDetail)
        {
            _vm.MovieDetail.BackCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.Mode == ContentMode.LiveTv)
        {
            _vm.GoBackCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && (_vm.Mode == ContentMode.Movies || _vm.Mode == ContentMode.Series))
        {
            _vm.GoBackCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F && e.Source is not TextBox)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Space && e.Source is not TextBox)
        {
            LogService.Info("Space pressed", new { mode = _vm.Mode.ToString(), isPlaying = _vm.IsPlaying, isPaused = _vm.IsPaused });
            _vm.TogglePlayPauseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.M && e.Source is not TextBox)
        {
            _vm.ToggleMuteCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.D && e.Source is not TextBox)
        {
            _vm.ShowDebugOverlay = !_vm.ShowDebugOverlay;
            e.Handled = true;
        }
    }

    private void OnTransportEntered(object? sender, PointerEventArgs e)
    {
        TransportBar.Opacity = 1;
        _transportAutoHide.Stop();
    }

    private void OnTransportExited(object? sender, PointerEventArgs e)
    {
        _transportAutoHide.Stop();
        _transportAutoHide.Start();
    }

    private void OnVolumeBtnEntered(object? sender, PointerEventArgs e)
    {
        _volumeHideTimer.Stop();
        _transportAutoHide.Stop();
        _vm.ShowVolumePopup = true;
        PositionVolumeOverlay();
    }

    // Anchor the slider popup over the volume button so it tracks the button's actual position,
    // not the window edge — adding/removing transport buttons no longer shifts it out of alignment.
    private void PositionVolumeOverlay()
    {
        if (VolumeOverlay.Parent is not Visual parent) return;
        if (VolumeBtn.TranslatePoint(new Point(0, 0), parent) is not { } pt) return;
        const double overlayWidth = 86; // canvas + slider + padding
        var left = pt.X + VolumeBtn.Bounds.Width / 2 - overlayWidth / 2;
        VolumeOverlay.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        VolumeOverlay.Margin = new Thickness(System.Math.Max(left, 4), 0, 0, 52);
    }

    private void OnVolumeBtnExited(object? sender, PointerEventArgs e)
    {
        _volumeHideTimer.Stop();
        _volumeHideTimer.Start();
    }

    private void OnVolumePopupEntered(object? sender, PointerEventArgs e)
    {
        _volumeHideTimer.Stop();
        _transportAutoHide.Stop();
    }

    private void OnVolumePopupExited(object? sender, PointerEventArgs e)
    {
        _volumeHideTimer.Stop();
        _volumeHideTimer.Start();
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

    private void OnAudioClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _vm.RefreshAudioTracks();
        var menu = new ContextMenu();
        foreach (var t in _vm.AudioTrackItems)
        {
            var mi = new MenuItem { Header = t.Name, Tag = t.Id };
            mi.Click += (_, _) =>
            {
                _vm.SelectAudioCommand.Execute(t.Id);
                menu.Close();
            };
            menu.Items.Add(mi);
        }
        if (menu.Items.Count == 0)
            menu.Items.Add(new MenuItem { Header = "No audio tracks", IsEnabled = false });
        menu.Open(btn);
    }

    private void OnCastClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        // Toggle: a click while the menu is open light-dismisses it first, then fires here — so if it
        // just closed, treat this click as the "close" and don't reopen.
        if ((DateTime.UtcNow - _castMenuClosedAt).TotalMilliseconds < 250) return;
        _vm.RescanCastDevices();   // SharpCaster scan is one-shot; re-scan each time the menu opens
        var menu = new ContextMenu();
        // Keep the transport bar up while the (possibly slow) device list / "searching…" box is open,
        // and resume the auto-hide countdown once it closes. The flag defeats PointerExited (moving the
        // mouse onto the menu leaves the bar) restarting the hide timer.
        _castMenuOpen = true;
        _transportAutoHide.Stop();
        TransportBar.Opacity = 1;
        menu.Closed += (_, _) => { _castMenuOpen = false; _castMenuClosedAt = DateTime.UtcNow; ShowTransport(); };
        if (_vm.IsCasting)
        {
            var stop = new MenuItem { Header = $"Stop casting — play here" };
            stop.Click += (_, _) => { _vm.StopCastingCommand.Execute(null); menu.Close(); };
            menu.Items.Add(stop);
            menu.Items.Add(new Separator());
        }
        var targets = _vm.CastTargets;
        for (var i = 0; i < targets.Count; i++)
        {
            var index = i;
            var mi = new MenuItem { Header = targets[i] };
            mi.Click += (_, _) => { _vm.CastToCommand.Execute(index); menu.Close(); };
            menu.Items.Add(mi);
        }
        if (targets.Count == 0)
            menu.Items.Add(new MenuItem { Header = "Searching for devices…", IsEnabled = false });
        menu.Open(btn);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && WindowState == WindowState.Minimized)
        {
            TransportBar.Opacity = 0;
            _transportAutoHide.Stop();
        }
    }

    // Restore the last windowed size; ignore junk/too-small saved values. Size only — position is
    // left to default centering so the window can't be restored off-screen.
    private void RestoreWindowSize()
    {
        var s = _vm.GetSettings();
        if (s.WindowWidth >= 400 && s.WindowHeight >= 300)
        {
            Width = s.WindowWidth;
            Height = s.WindowHeight;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Only persist a real windowed size — not a maximized/fullscreen/minimized frame.
        if (WindowState == WindowState.Normal)
            _vm.SaveWindowSize(ClientSize.Width, ClientSize.Height);
        _popout?.Close();
        _vm.SaveVodProgress();
        _vm.DebugStats.Stop();
        _player.Dispose();
        ImageService.Shutdown();
        _vm.GetUpdateService().ApplyPendingOnExit(restart: false);
        base.OnClosed(e);
    }
}
