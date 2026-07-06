using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LibVLCSharp.Shared;

namespace Sabeltann.Services;

public class PlaybackService : IDisposable
{
    private static PlaybackService? _instance;

    private readonly LibVLC _libVlc;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private Media? _currentMedia;
    private GCHandle _bufferHandle;
    private byte[] _frameBuffer = [];
    private byte[] _displayBuffer = [];
    private int _frameWidth;
    private int _frameHeight;
    private bool _disposed;
    private volatile int _framePending;
    private int _frameCount;
    private string? _currentUrl;

    private RendererDiscoverer? _rendererDiscoverer;
    private readonly List<RendererItem> _renderers = [];
    private RendererItem? _activeRenderer;
    private string? _castIp;      // Chromecast IP when casting via libvlc's own chromecast output
    private string? _castName;

    private static readonly MediaPlayer.LibVLCVideoLockCb LockCb = OnLock;
    private static readonly MediaPlayer.LibVLCVideoUnlockCb UnlockCb = OnUnlock;
    private static readonly MediaPlayer.LibVLCVideoDisplayCb DisplayCb = OnDisplay;
    private static readonly MediaPlayer.LibVLCVideoFormatCb FormatCb = OnFormat;
    private static readonly MediaPlayer.LibVLCVideoCleanupCb CleanupCb = OnCleanup;

    public bool IsPlaying => _mediaPlayer.IsPlaying;
    public long TimeMs => _mediaPlayer.Time;
    public long Length => _mediaPlayer.Length;
    public float Position => _mediaPlayer.Position;

    public WriteableBitmap? VideoBitmap { get; private set; }
    public int VideoWidth => _frameWidth;
    public int VideoHeight => _frameHeight;
    public int FramesDecoded => _frameCount;

    public Action? FrameRendered;
    public event EventHandler<string>? Error;
    public event EventHandler<int>? Buffering;
    public event EventHandler? PlayingStarted;
    public event EventHandler? Stopped;
    public event EventHandler? CastTargetsChanged;

    public PlaybackService()
    {
        Core.Initialize();
        _libVlc = new LibVLC("--network-caching=2000", "--no-video-title-show", "--no-keyboard-events");
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
        _mediaPlayer.EnableKeyInput = false;
        _mediaPlayer.EnableMouseInput = false;

        _instance = this;

        _mediaPlayer.SetVideoFormatCallbacks(FormatCb, CleanupCb);
        _mediaPlayer.SetVideoCallbacks(LockCb, UnlockCb, DisplayCb);

        _mediaPlayer.Buffering += (_, e) => Buffering?.Invoke(this, (int)e.Cache);
        _mediaPlayer.Playing += (_, _) => PlayingStarted?.Invoke(this, EventArgs.Empty);
        _mediaPlayer.Stopped += (_, _) => Stopped?.Invoke(this, EventArgs.Empty);
        _mediaPlayer.EncounteredError += (_, _) =>
        {
            LogService.Error("VLC playback error", new { url = _currentMedia?.Mrl });
            Error?.Invoke(this, "Playback failed");
        };
    }

    public void Play(string url)
    {
        Stop();

        var cleanUrl = CleanUrl(url);
        if (string.IsNullOrEmpty(cleanUrl))
        {
            LogService.Error("Invalid URL", new { url });
            Error?.Invoke(this, "Invalid URL");
            return;
        }

        LogService.Info("Playing", new { url = cleanUrl });

        // New media → forget any track baked in for the previous stream (cast track-select restarts
        // re-Play the same url and rely on these persisting; a real channel/movie change resets them).
        if (url != _currentUrl) { _selAudioTrackId = -1; _selSubTrackId = -1; }
        _currentUrl = url;
        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, new Uri(cleanUrl));
        _currentMedia.AddOption(":network-caching=2000");
        // While casting, the track must be selected at the input before the transcode/sout chain —
        // SetAudioTrack/SetSpu after Play don't survive the cast pipeline rebuild. Local playback
        // leaves these at -1 and switches live instead.
        if (_selAudioTrackId >= 0) _currentMedia.AddOption($":audio-track-id={_selAudioTrackId}");
        if (_selSubTrackId >= 0) _currentMedia.AddOption($":sub-track-id={_selSubTrackId}");
        // Casting: route output to the Chromecast via libvlc's own chromecast module (by IP, so no mDNS
        // needed). libvlc connects, launches the receiver and transcodes as needed — handles HEVC too.
        if (_castIp is not null)
        {
            _currentMedia.AddOption($":sout=#chromecast{{ip={_castIp},port=8009}}");
            _currentMedia.AddOption(":sout-keep");
            _currentMedia.AddOption(":demux-filter=demux_chromecast");
        }

        if (!_mediaPlayer.Play(_currentMedia))
        {
            LogService.Warn("Embedded playback failed, falling back to external VLC", new { url = cleanUrl });
            Error?.Invoke(this, "Embedded playback failed, launching external VLC");
            LaunchExternalVlc(cleanUrl);
        }
    }

    public void Stop()
    {
        _currentMedia?.Dispose();
        _currentMedia = null;
        // A paused decoder can swallow Stop() with custom video callbacks; resume first.
        if (_mediaPlayer.State == VLCState.Paused)
            _mediaPlayer.SetPause(false);
        _mediaPlayer.Stop();
        FreeBuffer();
    }

    public void Pause() => _mediaPlayer.Pause();
    public void SetPause(bool pause) => _mediaPlayer.SetPause(pause);

    public void Seek(int deltaMs) => _mediaPlayer.Time += deltaMs;
    public void SetPosition(TimeSpan position) => _mediaPlayer.Time = (long)position.TotalMilliseconds;
    public void SetPositionPercent(float position) => _mediaPlayer.Position = position;

    public void SetVolume(int volume) => _mediaPlayer.Volume = Math.Clamp(volume, 0, 200);
    public bool IsMuted => _mediaPlayer.Mute;
    public void MuteAudio(bool mute) => _mediaPlayer.Mute = mute;
    public void ToggleMute() => _mediaPlayer.ToggleMute();

    public List<(int Id, string Name)> GetSubtitleTracks()
    {
        var tracks = _mediaPlayer.SpuDescription;
        return tracks is null ? [] : tracks.Select(t => (t.Id, t.Name)).ToList();
    }

    public int CurrentSubtitleTrack => _mediaPlayer.Spu;
    public void SetSubtitleTrack(int id) => _mediaPlayer.SetSpu(id);

    public List<(int Id, string Name)> GetAudioTracks()
    {
        var tracks = _mediaPlayer.AudioTrackDescription;
        return tracks is null
            ? []
            : tracks.Where(t => t.Id != -1).Select(t => (t.Id, t.Name)).ToList();
    }

    public int CurrentAudioTrack => _mediaPlayer.AudioTrack;
    public void SetAudioTrack(int id) => _mediaPlayer.SetAudioTrack(id);

    private int _selAudioTrackId = -1;
    private int _selSubTrackId = -1;

    /// <summary>Casting only: bake the audio track into media options and restart the cast stream.</summary>
    public void SetCastAudioTrack(int id) { _selAudioTrackId = id; RestartCurrent(); }

    /// <summary>Casting only: bake the subtitle track (-1 = off) into media options and restart.</summary>
    public void SetCastSubtitleTrack(int id) { _selSubTrackId = id; RestartCurrent(); }

    private void RestartCurrent()
    {
        var url = _currentUrl;
        if (url is null) return;
        Stop();            // disposes media; _currentUrl (field) persists so Play keeps the baked tracks
        Play(url);
    }

    public bool IsCasting => _activeRenderer is not null || _castIp is not null;
    public string? CastTargetName => _castName ?? _activeRenderer?.Name;

    /// <summary>Casts the current stream to the Chromecast at <paramref name="ip"/> using libvlc's own
    /// chromecast output — transcodes as needed (handles HEVC), runs its own HTTP server, no mDNS.
    /// The IP comes from the unicast device scan. Restarts the current stream to apply the sout.</summary>
    public void CastToIp(string ip, string deviceName)
    {
        _castIp = ip;
        _castName = deviceName;
        RestartCurrent();
    }

    /// <summary>Cast-capable renderers found so far (Chromecasts on the LAN). Names only.</summary>
    public IReadOnlyList<string> CastTargets => _renderers.Select(r => r.Name).ToList();

    /// <summary>Begins mDNS renderer discovery; idempotent. No-op if libvlc ships no discoverer.</summary>
    // KNOWN LIMITATION: libvlc's libmicrodns binds mDNS to the wrong interface when a Tailscale (or
    // any VPN TUN) adapter is present, so no Chromecasts are found — even though the device is
    // reachable on the LAN. `tailscale down` is NOT enough; the adapter must be disabled/removed.
    // LibVLCSharp's RendererDiscoverer exposes no interface knob, and a RendererItem can only come
    // from libvlc's own discoverer, so there's no in-app fix. Verified 2026-06-30: disabling the
    // Tailscale adapter makes the TV appear instantly. ponytail: documented, not coded around —
    // revisit only if we move off libvlc-native casting.
    public void StartCastDiscovery()
    {
        if (_rendererDiscoverer is not null) return;
        var desc = _libVlc.RendererList.FirstOrDefault();
        if (string.IsNullOrEmpty(desc.Name))
        {
            LogService.Warn("Cast discovery: no renderer module in this libvlc build");
            return;
        }
        _rendererDiscoverer = new RendererDiscoverer(_libVlc, desc.Name);
        _rendererDiscoverer.ItemAdded += OnRendererAdded;
        _rendererDiscoverer.ItemDeleted += OnRendererDeleted;
        _rendererDiscoverer.Start();
    }

    private void OnRendererAdded(object? sender, RendererDiscovererItemAddedEventArgs e)
    {
        _renderers.Add(e.RendererItem);
        CastTargetsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnRendererDeleted(object? sender, RendererDiscovererItemDeletedEventArgs e)
    {
        _renderers.RemoveAll(r => r.Name == e.RendererItem.Name);
        CastTargetsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Routes playback to the renderer at <paramref name="index"/> (-1 = back to local) and restarts the current stream.</summary>
    public void CastTo(int index)
    {
        _activeRenderer = index >= 0 && index < _renderers.Count ? _renderers[index] : null;
        // SetRenderer only takes effect on the next Play, so restart the current stream.
        var url = _currentUrl;
        Stop();
        _mediaPlayer.SetRenderer(_activeRenderer);
        if (url is not null) Play(url);
    }

    public void StopCasting()
    {
        if (_castIp is not null) { _castIp = null; _castName = null; RestartCurrent(); return; }
        CastTo(-1);
    }

    /// <summary>Clears the cast target without restarting playback — for when playback stops entirely.</summary>
    public void ClearRenderer()
    {
        _activeRenderer = null;
        _castIp = null;
        _castName = null;
        _mediaPlayer.SetRenderer(null);
    }

    /// <summary>
    /// Zeros the video surface so the last decoded frame doesn't linger when switching streams.
    /// Safe to call from any thread; posts to the UI thread if the bitmap exists.
    /// </summary>
    public void ClearBitmap()
    {
        var bitmap = VideoBitmap;
        if (bitmap is null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var b = VideoBitmap;
            if (b is null) return;
            try
            {
                using var locked = b.Lock();
                unsafe
                {
                    var ptr = (byte*)locked.Address.ToPointer();
                    var byteCount = locked.RowBytes * locked.Size.Height;
                    new Span<byte>(ptr, (int)byteCount).Clear();
                }
            }
            catch { }
        }, DispatcherPriority.Render);
    }

    /// <summary>Current LibVLC media statistics, or null if no media is active.</summary>
    public MediaStats? GetStats() => _mediaPlayer.Media?.Statistics;

    /// <summary>Bytes read from the source for the current media. Stays ~0 for a stream that connects but delivers nothing — the liveness signal while casting, where no local frames decode.</summary>
    public long InputBytes => GetStats()?.ReadBytes ?? 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _rendererDiscoverer?.Dispose();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        if (_instance == this) _instance = null;
        GC.SuppressFinalize(this);
    }

    private static PlaybackService? Self => _instance is { _disposed: false } ? _instance : null;

    private static IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        try
        {
            var self = Self;
            if (self is null || self._frameBuffer.Length == 0) return IntPtr.Zero;
            var handle = GCHandle.Alloc(self._frameBuffer, GCHandleType.Pinned);
            self._bufferHandle = handle;
            Marshal.WriteIntPtr(planes, handle.AddrOfPinnedObject());
            return handle.AddrOfPinnedObject();
        }
        catch { return IntPtr.Zero; }
    }

    private static void OnUnlock(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        try
        {
            var self = Self;
            if (self is null) return;
            if (self._bufferHandle.IsAllocated)
                self._bufferHandle.Free();
        }
        catch { }
    }

    private static void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        try
        {
            var self = Self;
            if (self is null || self._disposed) return;

            self._frameCount++;
            if ((self._frameCount % 60) == 0)
                Console.Error.WriteLine($"OnDisplay: {self._frameCount} frames, bitmap={self.VideoBitmap is not null}");

            if (self._displayBuffer.Length > 0)
                Buffer.BlockCopy(self._frameBuffer, 0, self._displayBuffer, 0, self._displayBuffer.Length);

            if (Interlocked.Exchange(ref self._framePending, 1) == 0)
                Dispatcher.UIThread.Post(self.RenderFrame, DispatcherPriority.Render);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"OnDisplay error: {ex.Message}");
        }
    }

    private static uint OnFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        try
        {
            var self = Self;
            if (self is null) return 0;

            Console.Error.WriteLine($"OnFormat: w={width} h={height}");

            Marshal.WriteByte(chroma, 0, (byte)'R');
            Marshal.WriteByte(chroma, 1, (byte)'V');
            Marshal.WriteByte(chroma, 2, (byte)'3');
            Marshal.WriteByte(chroma, 3, (byte)'2');

            var w = (int)width;
            var h = (int)height;
            var stride = width * 4;
            self._frameWidth = w;
            self._frameHeight = h;
            pitches = stride;
            lines = height;
            self._frameBuffer = new byte[stride * height];
            self._displayBuffer = new byte[stride * height];

            Dispatcher.UIThread.Post(() =>
            {
                self.VideoBitmap = new WriteableBitmap(
                    new PixelSize(w, h),
                    new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Opaque);
            }, DispatcherPriority.Normal);

            return 1;
        }
        catch { return 0; }
    }

    private static void OnCleanup(ref IntPtr opaque)
    {
        try { Self?.FreeBuffer(); }
        catch { }
    }

    private void FreeBuffer()
    {
        if (_bufferHandle.IsAllocated)
            _bufferHandle.Free();
    }

    private void RenderFrame()
    {
        if (_disposed || VideoBitmap is null)
        {
            _framePending = 0;
            return;
        }

        try
        {
            using var locked = VideoBitmap.Lock();
            var srcStride = _frameWidth * 4;
            var dstStride = (int)locked.RowBytes;
            var height = locked.Size.Height;
            if (dstStride == srcStride)
            {
                Marshal.Copy(_displayBuffer, 0, locked.Address, _displayBuffer.Length);
            }
            else
            {
                for (var y = 0; y < height; y++)
                    Marshal.Copy(_displayBuffer, y * srcStride, locked.Address + y * dstStride, srcStride);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RenderFrame error: {ex.Message}");
        }

        _framePending = 0;

        FrameRendered?.Invoke();
    }

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        var u = url.Trim().Replace('\\', '/');
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
            u = "http://" + u;
        if (Uri.TryCreate(u, UriKind.Absolute, out var uri))
            return uri.AbsoluteUri;
        return u;
    }

    private static void LaunchExternalVlc(string url)
    {
        try
        {
            var vlc = FindVlcPath();
            if (vlc is not null)
                Process.Start(new ProcessStartInfo { FileName = vlc, ArgumentList = { url }, UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    private static string? FindVlcPath()
    {
        if (!OperatingSystem.IsWindows()) return null;
        var paths = new[]
        {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
            @"%LOCALAPPDATA%\Programs\VideoLAN\VLC\vlc.exe",
        };
        foreach (var p in paths.Select(Environment.ExpandEnvironmentVariables))
            if (File.Exists(p)) return p;
        return null;
    }
}
