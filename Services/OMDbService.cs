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
    string? PosterUrl
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

    private readonly string? _apiKey;
    private readonly HttpClient _http;

    public OMDbService(string? apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<OMDbResult?> FetchAsync(string title, string? year, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        var cacheKey = ComputeCacheKey(title, year);
        var cached = TryReadCache(cacheKey);
        if (cached is not null)
            return cached;

        var url = $"http://www.omdbapi.com/?apikey={_apiKey}&t={Uri.EscapeDataString(title)}&y={year ?? ""}&plot=full";

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
                PosterUrl: NormalizeNa(poster)
            );

            WriteCache(cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            LogService.Error("OMDbService.FetchAsync failed", new { title, error = ex.Message });
            return null;
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
            if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddHours(-24))
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
}
