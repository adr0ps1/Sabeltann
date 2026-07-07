using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sabeltann.Services;

namespace Sabeltann;

public record OMDbResult(
    string? ImdbRating,
    string? RottenTomatoes,
    string? Runtime,
    string? Genre,
    string? Plot,
    string? Actors,
    string? Director,
    string? PosterUrl,
    string? Language,
    string? Released = null
);

public sealed class OMDbService
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sabeltann", "omdbcache");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ponytail: shared cap so a screenful of poster lookups doesn't burst the API at once.
    private static readonly SemaphoreSlim NetThrottle = new(4, 4);

    private string? _apiKey;
    private readonly HttpClient _http;

    public OMDbService(string? apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>Updates the key in place so existing holders pick it up (no instance churn).</summary>
    public void SetApiKey(string? apiKey) => _apiKey = apiKey;

    // In-memory memo of cache reads so sorting a whole grid by rating doesn't re-stat the
    // disk cache for every title on each re-sort. Populated on read and on WriteCache.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, OMDbResult?> MemCache = new();

    /// <summary>
    /// Returns already-cached OMDb data for a title without ever hitting the network — used by
    /// rating/date sorting, where titles the user hasn't opened yet simply have no data and sink.
    /// </summary>
    public OMDbResult? TryGetCached(string title, string? year)
        => MemCache.GetOrAdd(ComputeCacheKey(title, year), TryReadCache);

    public async Task<OMDbResult?> FetchAsync(string title, string? year, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        var cacheKey = ComputeCacheKey(title, year);
        var cached = TryReadCache(cacheKey);
        if (cached is not null)
            return cached;

        var url = $"http://www.omdbapi.com/?apikey={_apiKey}&t={Uri.EscapeDataString(title)}&y={year ?? ""}&plot=full";

        await NetThrottle.WaitAsync(ct);
        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Response", out var responseProp) ||
                responseProp.GetString() != "True")
                return null;

            string? imdbRating = GetStringOrNull(root, "imdbRating");
            string? runtime = GetStringOrNull(root, "Runtime");
            string? genre = GetStringOrNull(root, "Genre");
            string? plot = GetStringOrNull(root, "Plot");
            string? actors = GetStringOrNull(root, "Actors");
            string? director = GetStringOrNull(root, "Director");
            string? poster = GetStringOrNull(root, "Poster");
            string? language = GetStringOrNull(root, "Language");
            string? released = GetStringOrNull(root, "Released");

            string? rottenTomatoes = null;
            if (root.TryGetProperty("Ratings", out var ratingsEl) &&
                ratingsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var rating in ratingsEl.EnumerateArray())
                {
                    if (rating.TryGetProperty("Source", out var src) &&
                        src.GetString() == "Rotten Tomatoes" &&
                        rating.TryGetProperty("Value", out var val))
                    {
                        rottenTomatoes = val.GetString();
                        break;
                    }
                }
            }

            var result = new OMDbResult(
                ImdbRating: NormalizeNa(imdbRating),
                RottenTomatoes: NormalizeNa(rottenTomatoes),
                Runtime: NormalizeNa(runtime),
                Genre: NormalizeNa(genre),
                Plot: NormalizeNa(plot),
                Actors: NormalizeNa(actors),
                Director: NormalizeNa(director),
                PosterUrl: NormalizeNa(poster),
                Language: NormalizeNa(language),
                Released: NormalizeNa(released)
            );

            WriteCache(cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            LogService.Error("OMDbService.FetchAsync failed", new { title, error = ex.Message });
            return null;
        }
        finally
        {
            NetThrottle.Release();
        }
    }

    private static string ComputeCacheKey(string title, string? year)
    {
        var input = title + "|" + (year ?? "");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static OMDbResult? TryReadCache(string cacheKey)
    {
        try
        {
            var path = Path.Combine(CacheDir, $"{cacheKey}.json");
            if (!File.Exists(path))
                return null;
            // Long TTL: ratings/release dates barely change, and a short window would re-burn the
            // 1000/day OMDb quota every time a grid is re-sorted.
            if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-30))
                return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<OMDbResult>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCache(string cacheKey, OMDbResult result)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var path = Path.Combine(CacheDir, $"{cacheKey}.json");
            var json = JsonSerializer.Serialize(result, JsonOpts);
            File.WriteAllText(path, json);
            MemCache[cacheKey] = result; // keep the sort memo fresh after a lazy fetch
        }
        catch (Exception ex)
        {
            LogService.Error("OMDbService.WriteCache failed", new { cacheKey, error = ex.Message });
        }
    }

    private static string? GetStringOrNull(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var el) ? el.GetString() : null;

    private static string? NormalizeNa(string? value)
        => value is null || value.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : value;

    // ---- Sort-key parsing (shared by the Movies/Series rating & date sorts) ----

    /// <summary>IMDb 0-10 score as a double, or null if absent/unparsable.</summary>
    public static double? ParseImdb(string? imdbRating)
        => double.TryParse(imdbRating, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Rotten Tomatoes "85%" as 0-100, or null.</summary>
    public static double? ParseRottenTomatoes(string? rt)
        => rt is not null && rt.EndsWith('%') && int.TryParse(rt[..^1], out var v) ? v : null;

    /// <summary>Combined 0-5 star score: average of whichever of IMDb / RT is present; null if neither.</summary>
    public static double? CombinedStars(OMDbResult? r)
    {
        double sum = 0; int n = 0;
        if (ParseImdb(r?.ImdbRating) is double i) { sum += i / 10 * 5; n++; }
        if (ParseRottenTomatoes(r?.RottenTomatoes) is double rt) { sum += rt / 100 * 5; n++; }
        return n > 0 ? sum / n : null;
    }

    /// <summary>OMDb "13 Jul 2018" release string as a date, or null.</summary>
    public static DateTime? ParseReleased(string? released)
        => DateTime.TryParse(released, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : null;
}

/// <summary>
/// Rating/date sort shared by the Movies and Series browsers. Titles with a sort key (a cached
/// OMDb rating, or a year for date sort) come first, ordered descending; everything without a key
/// keeps its original order at the bottom — so an un-fetched grid degrades to provider order
/// instead of burning the OMDb quota.
/// </summary>
public static class VodSorting
{
    public const string Default = "Default";
    public static readonly string[] Options = { Default, "IMDb", "Rotten Tomatoes", "Combined ★", "Release date" };

    public static IEnumerable<T> Apply<T>(IEnumerable<T> items, string? sort, OMDbService omdb,
        Func<T, string> name, Func<T, string?> year)
    {
        if (string.IsNullOrEmpty(sort) || sort == Default) return items;

        Func<T, double?> key = sort switch
        {
            "IMDb" => t => OMDbService.ParseImdb(omdb.TryGetCached(name(t), year(t))?.ImdbRating),
            "Rotten Tomatoes" => t => OMDbService.ParseRottenTomatoes(omdb.TryGetCached(name(t), year(t))?.RottenTomatoes),
            "Combined ★" => t => OMDbService.CombinedStars(omdb.TryGetCached(name(t), year(t))),
            "Release date" => t => ReleaseKey(omdb, name(t), year(t)),
            _ => _ => null,
        };

        var keyed = items.Select(t => (item: t, k: key(t))).ToList();
        return keyed.Where(x => x.k is not null).OrderByDescending(x => x.k!.Value).Select(x => x.item)
            .Concat(keyed.Where(x => x.k is null).Select(x => x.item));
    }

    // Prefer the cached OMDb release date; fall back to the provider year (free, so date sort
    // doesn't sink titles just because OMDb hasn't been fetched). Encoded as ticks for ordering.
    private static double? ReleaseKey(OMDbService omdb, string name, string? year)
    {
        if (OMDbService.ParseReleased(omdb.TryGetCached(name, year)?.Released) is DateTime d)
            return d.Ticks;
        if (int.TryParse(year, out var y) && y > 1800)
            return new DateTime(y, 1, 1).Ticks;
        return null;
    }
}
