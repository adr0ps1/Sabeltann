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
    private readonly DispatcherTimer _transportTimer = new();

    public MainWindow()
    {
        InitializeComponent();
        LogService.Info("Application started");
        _player = new PlaybackService();
        _vm = new MainViewModel();
        _vm.SetPlayer(_player);
        _vm.ToggleFullscreenRequested += ToggleFullscreen;
        DataContext = _vm;

        VideoView.Attach(_player.Player);
        KeyDown += OnKeyDown;

        _transportTimer.Interval = TimeSpan.FromSeconds(3);
        _transportTimer.Tick += OnTransportTimerTick;

        PointerMoved += OnWindowPointerMoved;
        TransportOverlay.PointerMoved += OnTransportPointerMoved;

        LoadPirateIcon();
        _vm.LoadLastSession();

        ConnectionPage.LoadM3UFileRequested += OnLoadM3UFile;
        ConnectionPage.LoadM3UUrlRequested += OnLoadM3UUrl;
        ConnectionPage.XtreamLoginRequested += OnXtreamLogin;

        ContentPicker.LiveTvSelected += async (_, _) =>
        {
            if (_vm.HasContent)
                _vm.ShowLiveChannels();
            else
                await _vm.ShowPlaylistContentAsync();
            _vm.ShowGroupsList = false;
        };
        ContentPicker.VodSelected += async (_, _) =>
        {
            if (_vm.HasContent)
                _vm.ShowVodChannels();
            else
                await _vm.ShowPlaylistContentAsync();
            _vm.ShowGroupsList = false;
        };
        ContentPicker.SearchRequested += async (_, query) =>
        {
            if (!_vm.HasContent)
                await _vm.ShowPlaylistContentAsync();
            _vm.SearchText = query;
            _vm.ShowGroupsList = false;
        };

        Opened += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
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

    private void OnWindowPointerMoved(object? sender, PointerEventArgs e)
    {
        ShowTransportTemporarily();
    }

    private void OnTransportPointerMoved(object? sender, PointerEventArgs e)
    {
        ShowTransportTemporarily();
    }

    private void ShowTransportTemporarily()
    {
        TransportOverlay.Opacity = 1;
        _transportTimer.Stop();
        _transportTimer.Start();
    }

    private void OnTransportTimerTick(object? sender, EventArgs e)
    {
        _transportTimer.Stop();
        TransportOverlay.Opacity = 0;
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = WindowState != WindowState.FullScreen;
        WindowState = _isFullscreen ? WindowState.FullScreen : WindowState.Normal;

        MainMenu.IsVisible = !_isFullscreen;
        CategoryBar.IsVisible = !_isFullscreen;
        SidebarPanel.IsVisible = !_isFullscreen;
        SidebarSplitter.IsVisible = !_isFullscreen;
        StatusBar.IsVisible = !_isFullscreen;

        if (_isFullscreen)
        {
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(0);
            ContentGrid.ColumnDefinitions[1].Width = new GridLength(0);
            TransportOverlay.Opacity = 0;
            _transportTimer.Stop();
        }
        else
        {
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(280);
            ContentGrid.ColumnDefinitions[1].Width = new GridLength(5);
            TransportOverlay.Opacity = 1;
            _transportTimer.Stop();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F)
        {
            ToggleFullscreen();
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

    private void OnTransportEntered(object? sender, PointerEventArgs e)
    {
        if (_isFullscreen) _transportTimer.Stop();
    }

    private void OnTransportExited(object? sender, PointerEventArgs e)
    {
        if (_isFullscreen) ShowTransportTemporarily();
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

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _transportTimer.Stop();
        _vm.DebugStats.Stop();
        VideoView.Detach();
        _player.Dispose();
        base.OnClosed(e);
    }
}
