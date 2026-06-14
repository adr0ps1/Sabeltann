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
    private bool _isFullscreen;

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
        KeyDown += OnKeyDown;

        var transportTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        transportTimer.Tick += (_, _) => { TransportPopup.IsOpen = false; transportTimer.Stop(); };
        PointerMoved += (_, _) =>
        {
            if (_vm.IsPlaying)
            {
                TransportPopup.IsOpen = true;
                transportTimer.Stop();
                transportTimer.Start();
            }
        };

        LoadPirateIcon();
        _vm.LoadLastSession();

        TransportPopup.PlacementTarget = VideoView;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsPlaying))
            {
                if (_vm.IsPlaying)
                {
                    TransportPopup.IsOpen = true;
                    transportTimer.Stop();
                    transportTimer.Start();
                }
                if (!_vm.IsPlaying && _isFullscreen)
                    ToggleFullscreen();
            }
        };

        Opened += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
        };

        ConnectionPage.LoadM3UFileRequested += OnLoadM3UFile;
        ConnectionPage.LoadM3UUrlRequested += OnLoadM3UUrl;
        ConnectionPage.XtreamLoginRequested += OnXtreamLogin;

        ContentPicker.LiveTvSelected += async (_, _) =>
        {
            if (!_vm.HasContent)
                await _vm.ShowPlaylistContentAsync();
            _vm.ShowLiveChannels();
            _vm.ShowGroupsList = false;
        };
        ContentPicker.VodSelected += async (_, _) =>
        {
            if (!_vm.HasContent)
                await _vm.ShowPlaylistContentAsync();
            _vm.ShowVodChannels();
            _vm.ShowGroupsList = false;
        };

        Opened += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
        };

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsPlaying) && !_vm.IsPlaying && _isFullscreen)
                ToggleFullscreen();
        };
    }

    private void LoadPirateIcon()
    {
        try
        {
            using var stream = typeof(MainWindow).Assembly
                .GetManifestResourceStream("Sabeltann.Assets.pirate.svg");
            if (stream is null) return;

            var svg = new Svg.Skia.SKSvg();
            var pic = svg.Load(stream);
            if (pic is null) return;

            var size = 64;
            var srcW = pic.CullRect.Width;
            var srcH = pic.CullRect.Height;
            var scale = size / Math.Max(srcW, srcH);

            using var bmp = new SkiaSharp.SKBitmap(size, size);
            using var canvas = new SkiaSharp.SKCanvas(bmp);
            canvas.Clear(SkiaSharp.SKColors.Transparent);

            var tx = (size - srcW * scale) / 2f;
            var ty = (size - srcH * scale) / 2f;
            canvas.Save();
            canvas.Translate(tx, ty);
            canvas.Scale(scale);
            canvas.DrawPicture(pic, SkiaSharp.SKPoint.Empty);
            canvas.Restore();

            using var img = SkiaSharp.SKImage.FromBitmap(bmp);
            using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            Icon = new WindowIcon(Avalonia.Media.Imaging.Bitmap.DecodeToWidth(ms, size));
        }
        catch { }
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = WindowState != WindowState.FullScreen;
        WindowState = _isFullscreen ? WindowState.FullScreen : WindowState.Normal;

        TitleBar.IsVisible = !_isFullscreen;
        MainMenu.IsVisible = !_isFullscreen;
        CategoryBar.IsVisible = !_isFullscreen;
        SidebarPanel.IsVisible = !_isFullscreen;
        SidebarSplitter.IsVisible = !_isFullscreen;
        StatusBar.IsVisible = !_isFullscreen;

        if (_isFullscreen)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(0);
            MainGrid.RowDefinitions[1].Height = new GridLength(0);
            MainGrid.RowDefinitions[3].Height = new GridLength(0);
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(0);
            ContentGrid.ColumnDefinitions[1].Width = new GridLength(1);
        }
        else
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(44);
            MainGrid.RowDefinitions[1].Height = GridLength.Auto;
            MainGrid.RowDefinitions[3].Height = new GridLength(28);
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(280);
            ContentGrid.ColumnDefinitions[1].Width = new GridLength(5);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            if (_vm.IsPlaying)
                _vm.TogglePlayPauseCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _vm.ShowContentPicker)
        {
            _vm.ShowContentPicker = false;
            _vm.StatusText = "Ready";
            e.Handled = true;
        }
        else if (e.Key == Key.D)
        {
            _vm.ShowDebugOverlay = !_vm.ShowDebugOverlay;
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

    protected override void OnClosed(EventArgs e)
    {
        _vm.DebugStats.Stop();
        VideoView.Detach();
        _player.Dispose();
        base.OnClosed(e);
    }
}

