using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sabeltann.Services;

/// <summary>
/// Records live TV while you keep watching, on a SINGLE upstream connection. A tiny loopback relay
/// pulls the provider stream once and tees the bytes to both:
///   (a) the player — which plays the relay's local URL, so video keeps rendering normally, and
///   (b) a <c>.ts</c> file.
/// Because the player reads the relay (not the provider), the on-screen preview is unaffected — unlike
/// a sout, which hijacks the video path. And there's only ever one connection to the provider. (#84)
///
/// ponytail: single client (the player). If libvlc drops+reconnects mid-recording the tee ends; that's
/// acceptable for "record what I'm watching". Upstream path/headers come straight from the channel URL.
/// </summary>
public sealed class RecordProxyService : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private HttpClient? _http;

    public bool IsRecording { get; private set; }
    public string? CurrentFile { get; private set; }

    /// <summary>Raised when the relay/upstream dies mid-recording (message for a toast).</summary>
    public event EventHandler<string>? Failed;

    public static string RecordingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Sabeltann");

    /// <summary>Start the relay and return a loopback URL for the player to play, or null on failure.</summary>
    public string? Start(string upstreamUrl)
    {
        if (IsRecording || string.IsNullOrWhiteSpace(upstreamUrl))
            return null;
        try
        {
            Directory.CreateDirectory(RecordingsFolder);
            CurrentFile = Path.Combine(RecordingsFolder, $"Sabeltann_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.ts");

            _listener = new TcpListener(IPAddress.Loopback, 0);   // ephemeral port, no admin/urlacl
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _cts = new CancellationTokenSource();
            _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })
            {
                Timeout = Timeout.InfiniteTimeSpan   // it's a live stream, never "completes"
            };

            IsRecording = true;
            _ = Task.Run(() => ServeAsync(upstreamUrl, _cts.Token));
            return $"http://127.0.0.1:{port}/live.ts";
        }
        catch (Exception ex)
        {
            LogService.Error("Record proxy start failed", new { error = ex.Message });
            Cleanup();
            return null;
        }
    }

    private async Task ServeAsync(string upstreamUrl, CancellationToken ct)
    {
        try
        {
            using var client = await _listener!.AcceptTcpClientAsync(ct);
            using var netStream = client.GetStream();

            // Read+discard the player's HTTP request (we don't care about its contents).
            var reqBuf = new byte[2048];
            await netStream.ReadAsync(reqBuf, ct);

            // Minimal streaming response: HTTP/1.0, no Content-Length → stream until the socket closes.
            var header = "HTTP/1.0 200 OK\r\nContent-Type: video/mp2t\r\nConnection: close\r\n\r\n";
            await netStream.WriteAsync(Encoding.ASCII.GetBytes(header), ct);

            using var file = new FileStream(CurrentFile!, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var upstream = await _http!.GetStreamAsync(upstreamUrl, ct);

            var buf = new byte[64 * 1024];
            int n;
            // Writing to the player first makes it the pace-setter (it reads at real time), which
            // naturally throttles our upstream read so the file stays in sync — no runaway download.
            while (!ct.IsCancellationRequested && (n = await upstream.ReadAsync(buf, ct)) > 0)
            {
                await netStream.WriteAsync(buf.AsMemory(0, n), ct);
                await file.WriteAsync(buf.AsMemory(0, n), ct);
            }
            await file.FlushAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) { /* normal stop */ }
        catch (Exception ex)
        {
            LogService.Warn("Record proxy stream ended", new { error = ex.Message });
            if (IsRecording)
                Failed?.Invoke(this, "Recording stopped — the stream ended or the connection dropped.");
        }
    }

    public void Stop()
    {
        if (!IsRecording) return;
        IsRecording = false;
        Cleanup();
        LogService.Info("Recording stopped", new { file = CurrentFile });
    }

    private void Cleanup()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _http?.Dispose();
        _cts?.Dispose();
        _listener = null;
        _http = null;
        _cts = null;
    }

    public void Dispose()
    {
        if (IsRecording) Stop();
        else Cleanup();
    }
}
