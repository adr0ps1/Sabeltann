using System;
using System.IO;
using LibVLCSharp.Shared;

namespace Sabeltann.Services;

/// <summary>
/// Records the current stream to a <c>.ts</c> file via its own headless LibVLC instance, fully
/// isolated from the on-screen player. Uses sout copy-mux (no transcode → keeps the original codecs,
/// incl. HEVC). Start/stop, no hard length limit. (#84)
///
/// ponytail: opens a *second* connection to the provider. Fine for multi-connection accounts; a
/// single-connection provider will fail to record (the main playback is unaffected either way).
/// Upgrade path: a duplicate-sout on the existing player (restarts the stream) if that becomes a need.
/// </summary>
public sealed class RecordingService : IDisposable
{
    private readonly LibVLC _libVlc;
    private MediaPlayer? _player;
    private Media? _media;

    public bool IsRecording { get; private set; }
    public string? CurrentFile { get; private set; }
    public DateTime StartedUtc { get; private set; }

    /// <summary>Raised (message) when a recording fails to start or dies mid-way.</summary>
    public event EventHandler<string>? Failed;

    public RecordingService()
    {
        Core.Initialize();
        // Dummy vout/aout: this instance must not open any window or play locally — it only muxes to file.
        _libVlc = new LibVLC("--vout=dummy", "--aout=dummy", "--no-video-title-show", "--network-caching=2000");
    }

    public static string RecordingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Sabeltann");

    /// <summary>Begin recording <paramref name="url"/> to a timestamped file. Returns false if it
    /// couldn't start (already recording, bad URL, or libvlc refused).</summary>
    public bool Start(string url)
    {
        if (IsRecording || string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            Directory.CreateDirectory(RecordingsFolder);
            var file = Path.Combine(RecordingsFolder, $"Sabeltann_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ts");
            var dst = file.Replace('\\', '/');   // libvlc sout dst wants forward slashes

            _media = new Media(_libVlc, new Uri(url));
            _media.AddOption(":network-caching=2000");
            _media.AddOption($":sout=#std{{access=file,mux=ts,dst='{dst}'}}");
            _media.AddOption(":sout-keep");

            _player = new MediaPlayer(_libVlc) { EnableKeyInput = false, EnableMouseInput = false };
            _player.EncounteredError += (_, _) =>
            {
                LogService.Error("Recording error", new { file });
                Failed?.Invoke(this, "Recording failed — the stream may only allow one connection.");
                Stop();
            };

            if (!_player.Play(_media))
            {
                Cleanup();
                return false;
            }

            CurrentFile = file;
            StartedUtc = DateTime.UtcNow;
            IsRecording = true;
            LogService.Info("Recording started", new { file });
            return true;
        }
        catch (Exception ex)
        {
            LogService.Error("Recording start failed", new { error = ex.Message });
            Cleanup();
            return false;
        }
    }

    public void Stop()
    {
        if (!IsRecording)
            return;
        try { _player?.Stop(); } catch { /* best-effort */ }
        LogService.Info("Recording stopped", new { file = CurrentFile });
        IsRecording = false;
        Cleanup();
    }

    private void Cleanup()
    {
        _media?.Dispose();
        _media = null;
        _player?.Dispose();
        _player = null;
    }

    public void Dispose()
    {
        if (IsRecording) Stop();
        _libVlc.Dispose();
    }
}
