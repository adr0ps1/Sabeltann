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

        _currentMedia?.Dispose();
        _currentMedia = new Media(_libVlc, new Uri(cleanUrl));
        _currentMedia.AddOption(":network-caching=2000");

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
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
        _framePending = 0;
        if (_disposed || VideoBitmap is null) return;

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
