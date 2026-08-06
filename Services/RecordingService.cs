using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sabeltann.Services;

/// <summary>
/// Records the currently-playing live/HLS stream to a .ts file WHILE it keeps playing.
///
/// libvlc's own recording (sout) consumes the stream and blanks our custom video surface, so instead
/// one ffmpeg process makes the single upstream connection and writes the stream to a .ts file. A tiny
/// loopback HTTP server then tails that same file to libvlc, which plays it over HTTP (its most reliable
/// transport) through the normal video path — so the picture stays on screen while recording, and the
/// provider still sees only one connection (ffmpeg's).
/// </summary>
public sealed class RecordingService : IDisposable
{
    private Process? _ffmpeg;
    private string? _outputPath;
    private int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRecording => _ffmpeg is { HasExited: false };
    public string? OutputPath => _outputPath;

    /// <summary>Fires when ffmpeg exits unexpectedly (crash, provider refused the connection, bad URL)
    /// while a recording was supposed to be running. Callers should fall back to direct playback.</summary>
    public event EventHandler? Failed;

    /// <summary>Locate/download ffmpeg up front (first run may download ~40MB) so we don't stall
    /// playback while it fetches. Returns false if ffmpeg is unavailable.</summary>
    public Task<bool> EnsureReadyAsync(IProgress<string>? status = null)
        => FfmpegLocator.EnsureAsync(status).ContinueWith(t => t.Result is not null);

    /// <summary>Reserve the loopback port + output file and return the URL libvlc should play.
    /// Call <see cref="StartAsync"/> next, then point the player at this URL.</summary>
    public string Prepare(string channelName)
    {
        _port = FreeTcpPort();
        _outputPath = BuildOutputPath(channelName);
        return $"http://127.0.0.1:{_port}/live.ts";
    }

    /// <summary>Start ffmpeg recording <paramref name="sourceUrl"/> to the reserved file and bring up the
    /// loopback HTTP server that streams it to the player. Returns false if ffmpeg can't launch.</summary>
    public async Task<bool> StartAsync(string sourceUrl)
    {
        if (IsRecording || _outputPath is null) return false;
        var ffmpeg = await FfmpegLocator.EnsureAsync();
        if (ffmpeg is null) return false;

        // -extension_picky 0: these IPTV HLS playlists point at extensionless segment URLs
        // (…/hls/<token>), which ffmpeg's HLS demuxer otherwise rejects ("not in
        // allowed_segment_extensions"). Single -f mpegts output handles the Windows path fine.
        var args = $"-hide_banner -loglevel error -fflags +genpts -extension_picky 0 " +
                   $"-i \"{sourceUrl}\" -c copy -map 0 -f mpegts \"{_outputPath}\"";
        try
        {
            _ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
            });
        }
        catch (Exception ex)
        {
            LogService.Error("ffmpeg start failed", new { error = ex.Message });
            _ffmpeg = null;
            return false;
        }
        if (_ffmpeg is null) return false;
        _ffmpeg.ErrorDataReceived += (_, e) => { if (e.Data is { Length: > 0 }) LogService.Info("ffmpeg", new { line = e.Data }); };
        _ffmpeg.BeginErrorReadLine();   // drain stderr so a full pipe can't block ffmpeg
        _ffmpeg.EnableRaisingEvents = true;
        _ffmpeg.Exited += (_, _) => { if (_listener is not null) Failed?.Invoke(this, EventArgs.Empty); };

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        return true;
    }

    /// <summary>Gracefully stop ffmpeg (flushing the file), tear down the server, return the saved path.</summary>
    public string? Stop()
    {
        var path = _outputPath;
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { /* already stopped */ }
        _listener = null;

        var p = _ffmpeg;
        _ffmpeg = null;
        if (p is not null)
        {
            try
            {
                if (!p.HasExited)
                {
                    p.StandardInput.Write('q');   // ffmpeg's graceful quit finalizes the .ts
                    p.StandardInput.Flush();
                    if (!p.WaitForExit(4000)) p.Kill();
                }
            }
            catch { try { p.Kill(); } catch { /* already gone */ } }
            finally { p.Dispose(); }
        }

        _cts?.Dispose();
        _cts = null;
        return path;
    }

    public void Dispose()
    {
        try { Stop(); } catch { }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = ServeAsync(client, ct);   // libvlc may open more than one connection; serve each
            }
        }
        catch { /* listener stopped / cancelled */ }
    }

    // Serves the growing .ts as an endless HTTP response: stream what ffmpeg has written, and when we
    // reach the current end, wait for more instead of sending EOF — so libvlc keeps playing live.
    private async Task ServeAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            client.NoDelay = true;
            using var _ = client;
            var net = client.GetStream();

            var reqBuf = new byte[4096];
            try { await net.ReadAsync(reqBuf, ct); } catch { /* ignore the request line/headers */ }

            var header = "HTTP/1.1 200 OK\r\n" +
                         "Content-Type: video/mp2t\r\n" +
                         "Cache-Control: no-cache\r\n" +
                         "Connection: close\r\n\r\n";
            await net.WriteAsync(Encoding.ASCII.GetBytes(header), ct);

            // Bounded wait: if ffmpeg dies before ever producing the file (bad URL, provider refused
            // the connection, codec error), give up instead of buffering the client forever.
            var waitTicks = 0;
            while (!ct.IsCancellationRequested && !File.Exists(_outputPath))
            {
                if (_ffmpeg is { HasExited: true } || ++waitTicks > 160) return;   // ~8s
                await Task.Delay(50, ct);
            }
            if (ct.IsCancellationRequested) return;

            using var fs = new FileStream(_outputPath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buf = new byte[64 * 1024];
            var idleTicks = 0;
            while (!ct.IsCancellationRequested)
            {
                var n = await fs.ReadAsync(buf, ct);
                if (n == 0)
                {
                    if (++idleTicks > 400) break;   // ~20s with no new bytes → ffmpeg died, drop client
                    await Task.Delay(50, ct);
                    continue;
                }
                idleTicks = 0;
                await net.WriteAsync(buf.AsMemory(0, n), ct);
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // client disconnected or we're shutting down — normal
        }
        catch (Exception ex)
        {
            LogService.Error("record relay serve error", new { error = ex.Message });
        }
    }

    private static int FreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static string BuildOutputPath(string channelName)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Sabeltann Recordings");
        Directory.CreateDirectory(dir);
        var safe = string.Concat((channelName ?? "recording").Split(Path.GetInvalidFileNameChars())).Trim();
        if (string.IsNullOrWhiteSpace(safe)) safe = "recording";
        return Path.Combine(dir, $"{safe}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ts");
    }
}

/// <summary>Finds ffmpeg.exe (bundled → downloaded cache → PATH) and, failing that, downloads a
/// static build to %LocalAppData%\Sabeltann\ffmpeg. Cached after the first successful resolve.</summary>
internal static class FfmpegLocator
{
    private const string DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _cached;

    private static string StoreDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sabeltann", "ffmpeg");

    public static async Task<string?> EnsureAsync(IProgress<string>? status = null)
    {
        if (_cached is not null && File.Exists(_cached)) return _cached;
        await Gate.WaitAsync();
        try
        {
            if (_cached is not null && File.Exists(_cached)) return _cached;

            var bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(bundled)) return _cached = bundled;

            var stored = Path.Combine(StoreDir, "ffmpeg.exe");
            if (File.Exists(stored)) return _cached = stored;

            var onPath = FromPath();
            if (onPath is not null) return _cached = onPath;

            return _cached = await DownloadAsync(stored, status);
        }
        finally { Gate.Release(); }
    }

    private static string? FromPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (path is null) return null;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            try
            {
                var f = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(f)) return f;
            }
            catch { /* malformed PATH entry */ }
        }
        return null;
    }

    private static async Task<string?> DownloadAsync(string dest, IProgress<string>? status)
    {
        try
        {
            status?.Report("Downloading recorder (first time only, ~40MB)…");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var zipPath = dest + ".download.zip";

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            await using (var src = await http.GetStreamAsync(DownloadUrl))
            await using (var f = File.Create(zipPath))
                await src.CopyToAsync(f);

            status?.Report("Extracting recorder…");
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                var entry = zip.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    LogService.Error("ffmpeg.exe not found in downloaded archive");
                    return null;
                }
                entry.ExtractToFile(dest, overwrite: true);
            }
            File.Delete(zipPath);
            return File.Exists(dest) ? dest : null;
        }
        catch (Exception ex)
        {
            LogService.Error("ffmpeg download failed", new { error = ex.Message });
            return null;
        }
    }
}
