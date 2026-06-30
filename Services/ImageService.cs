using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Sabeltann.Services;

public static class ImageService
{
    private const int MaxCacheEntries = 300;
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryTtl = TimeSpan.FromMinutes(5);
    private const int DecodeWidth = 300;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static readonly string DiskCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sabeltann", "imgcache");

    private static readonly Dictionary<string, Bitmap> Cache = new();
    private static readonly LinkedList<string> LruOrder = new();
    private static readonly object CacheLock = new();

    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> InFlight = new();
    private static readonly ConcurrentDictionary<string, FailureInfo> Failures = new();
    private static readonly SemaphoreSlim Throttle = new(6, 6);

    static ImageService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Sabeltann/1.0");
        try { Directory.CreateDirectory(DiskCacheDir); }
        catch { }
    }

    private record FailureInfo(int Count, DateTime LastAttempt);

    public static async Task<Bitmap?> LoadAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        lock (CacheLock)
        {
            if (Cache.TryGetValue(url, out var cached))
            {
                TouchLru(url);
                return cached;
            }
        }

        if (IsBlacklisted(url)) return null;

        var task = InFlight.GetOrAdd(url, _ => LoadInternalAsync(url, ct));
        try
        {
            return await task;
        }
        finally
        {
            InFlight.TryRemove(url, out _);
        }
    }

    /// <summary>Loads <paramref name="url"/> and hands the decoded bitmap to <paramref name="set"/>; no-op if it didn't decode.</summary>
    public static async Task LoadInto(string? url, Action<Bitmap> set, CancellationToken ct = default)
    {
        var bmp = await LoadAsync(url, ct);
        if (bmp is not null) set(bmp);
    }

    private static async Task<Bitmap?> LoadInternalAsync(string url, CancellationToken ct)
    {
        await Throttle.WaitAsync(ct);
        try
        {
            lock (CacheLock)
            {
                if (Cache.TryGetValue(url, out var cached))
                {
                    TouchLru(url);
                    return cached;
                }
            }

            var diskPath = GetDiskPath(url);
            byte[]? imageBytes = null;

            try
            {
                if (File.Exists(diskPath))
                    imageBytes = await File.ReadAllBytesAsync(diskPath, ct);
            }
            catch { }

            if (imageBytes is null)
            {
                try
                {
                    imageBytes = await Http.GetByteArrayAsync(url, ct);
                    try { await File.WriteAllBytesAsync(diskPath, imageBytes, ct); }
                    catch { }
                }
                catch (Exception ex)
                {
                    RecordFailure(url);
                    LogService.Warn("Image download failed", new { url, error = ex.Message });
                    return null;
                }
            }

            Bitmap? bmp = null;
            try
            {
                bmp = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var ms = new MemoryStream(imageBytes);
                    return Bitmap.DecodeToWidth(ms, DecodeWidth);
                });
            }
            catch
            {
                RecordFailure(url);
                return null;
            }

            if (bmp is null) return null;

            lock (CacheLock)
            {
                if (Cache.TryGetValue(url, out var existing))
                {
                    bmp.Dispose();
                    return existing;
                }
                Cache[url] = bmp;
                LruOrder.AddFirst(url);
                EvictIfNeeded();
            }

            Failures.TryRemove(url, out _);
            return bmp;
        }
        finally
        {
            Throttle.Release();
        }
    }

    private static void TouchLru(string url)
    {
        LruOrder.Remove(url);
        LruOrder.AddFirst(url);
    }

    private static void EvictIfNeeded()
    {
        while (Cache.Count > MaxCacheEntries && LruOrder.Count > 0)
        {
            var victim = LruOrder.Last!.Value;
            LruOrder.RemoveLast();
            // Do NOT dispose the bitmap here — ViewModels may still reference it.
            // The GC will collect it once all references are released.
            Cache.Remove(victim);
        }
    }

    private static bool IsBlacklisted(string url)
    {
        if (Failures.TryGetValue(url, out var info))
        {
            if (info.Count >= MaxRetries && DateTime.UtcNow - info.LastAttempt < RetryTtl)
                return true;
            Failures.TryRemove(url, out _);
        }
        return false;
    }

    private static void RecordFailure(string url)
    {
        Failures.AddOrUpdate(
            url,
            _ => new FailureInfo(1, DateTime.UtcNow),
            (_, info) => new FailureInfo(info.Count + 1, DateTime.UtcNow));
    }

    private static string GetDiskPath(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];
        return Path.Combine(DiskCacheDir, hash);
    }

    public static void Shutdown()
    {
        lock (CacheLock)
        {
            // Don't dispose bitmaps — ViewModels may still reference them during shutdown.
            // The process is ending, so the OS will reclaim the memory.
            Cache.Clear();
            LruOrder.Clear();
        }
        Failures.Clear();
        InFlight.Clear();
    }
}
