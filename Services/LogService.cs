using System.Diagnostics;
using System.Text.Json;

namespace Sabeltann.Services;

public static class LogService
{
    private static readonly string LogDir = Path.Combine(
        AppContext.BaseDirectory, "logs");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly object WriteLock = new();

    static LogService()
    {
        try { Directory.CreateDirectory(LogDir); }
        catch { }
    }

    public static void Info(string message, object? extra = null)
    {
        Write("info", message, extra);
    }

    public static void Warn(string message, object? extra = null)
    {
        Write("warn", message, extra);
    }

    public static void Error(string message, object? extra = null)
    {
        Write("error", message, extra);
    }

    private static void Write(string level, string message, object? extra)
    {
        string line;
        try
        {
            var entry = new Dictionary<string, object?>
            {
                ["ts"] = DateTime.UtcNow.ToString("O"),
                ["level"] = level,
                ["msg"] = message,
            };
            if (extra is not null)
                entry["extra"] = extra;

            line = JsonSerializer.Serialize(entry, JsonOpts) + Environment.NewLine;
        }
        catch
        {
            return;
        }

        var file = Path.Combine(LogDir, $"sabeltann-{DateTime.UtcNow:yyyy-MM-dd}.jsonl");

        lock (WriteLock)
        {
            try
            {
                File.AppendAllText(file, line);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[LogService] Failed to write log: {ex.Message}");
                Trace.WriteLine($"[LogService] Dropped entry: {line.Trim()}");
            }
        }
    }
}
