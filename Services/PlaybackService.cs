using System.Diagnostics;
using LibVLCSharp.Shared;

namespace Sabeltann.Services;

public class PlaybackService : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _currentMedia;

    public MediaPlayer Player => _mediaPlayer;
    public bool IsPlaying => _mediaPlayer.IsPlaying;
    public event EventHandler<string>? Error;
    public event EventHandler<int>? Buffering;
    public event EventHandler? PlayingStarted;
    public event EventHandler? Stopped;

    public PlaybackService()
    {
        try
        {
            Core.Initialize();
            _libVlc = new LibVLC("--network-caching=2000", "--no-video-title-show");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _mediaPlayer.Buffering += OnBuffering;
            _mediaPlayer.Playing += (_, _) => PlayingStarted?.Invoke(this, EventArgs.Empty);
            _mediaPlayer.Stopped += (_, _) => Stopped?.Invoke(this, EventArgs.Empty);
            _mediaPlayer.EncounteredError += (_, _) =>
            {
                LogService.Error("VLC playback error", new { url = _currentMedia?.Mrl });
                Error?.Invoke(this, "Playback failed");
            };
        }
        catch (Exception ex)
        {
            LogService.Error("VLC initialization failed", new { ex.Message });
        }
    }

    private void OnBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        Buffering?.Invoke(this, (int)e.Cache);
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
    }

    public void Pause()
    {
        _mediaPlayer.Pause();
    }

    public void Seek(int deltaMs)
    {
        _mediaPlayer.Time += deltaMs;
    }

    public void SetVolume(int volume)
    {
        _mediaPlayer.Volume = Math.Clamp(volume, 0, 200);
    }

    public bool IsMuted => _mediaPlayer.Mute;

    public void ToggleMute()
    {
        _mediaPlayer.ToggleMute();
    }

    public List<(int Id, string Name)> GetSubtitleTracks()
    {
        var tracks = _mediaPlayer.SpuDescription;
        if (tracks is null) return [];
        return tracks.Select(t => (t.Id, t.Name)).ToList();
    }

    public int CurrentSubtitleTrack => _mediaPlayer.Spu;

    public void SetSubtitleTrack(int id)
    {
        _mediaPlayer.SetSpu(id);
    }

    public void Dispose()
    {
        Stop();
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";

        // Normalize backslashes to forward slashes
        var u = url.Trim().Replace('\\', '/');

        // Ensure scheme
        if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
        {
            u = "http://" + u;
        }

        // Validate as absolute URI
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
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = vlc,
                    ArgumentList = { url },
                    UseShellExecute = true,
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                });
            }
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
        {
            if (File.Exists(p)) return p;
        }

        return null;
    }
}
