using System.Diagnostics;

namespace Sabeltann.Services;

/// <summary>
/// Windows' Programs &amp; Features reads an app's "Size" from an <c>EstimatedSize</c> value under its
/// uninstall registry key — but only when that value is a <c>REG_DWORD</c>. Velopack 1.2.0 writes it
/// as a <c>REG_QWORD</c>, which Windows ignores, so the Size column shows blank. This rewrites it as a
/// DWORD (in KB) on a normal launch, only when the uninstall key already exists — so dev runs and
/// non-installed launches never touch Programs &amp; Features.
/// ponytail: shells to reg.exe (always on Windows) to avoid a Windows-only TFM or a Registry package
/// for one value; switch to Microsoft.Win32.Registry if this needs to do more.
/// </summary>
public static class InstallerInfo
{
    private const string UninstallKey =
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Sabeltann";

    public static void EnsureEstimatedSize()
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;
            // Only a real Velopack install has this key.
            if (Reg("query", UninstallKey).exit != 0) return;

            // Leave a correct DWORD alone; fix a missing value or Velopack's REG_QWORD.
            var val = Reg("query", UninstallKey, "/v", "EstimatedSize");
            if (val.exit == 0 && val.stdout.Contains("REG_DWORD")) return;

            var kb = DirectorySizeKb(AppContext.BaseDirectory);
            if (kb <= 0) return;

            Reg("add", UninstallKey, "/v", "EstimatedSize", "/t", "REG_DWORD", "/d", kb.ToString(), "/f");
            LogService.Info("Fixed installer EstimatedSize (rewrote as REG_DWORD)", new { kb });
        }
        catch (Exception ex)
        {
            LogService.Warn("EnsureEstimatedSize failed", new { error = ex.Message });
        }
    }

    private static int DirectorySizeKb(string dir)
    {
        long bytes = new DirectoryInfo(dir)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(f => f.Length);
        return (int)Math.Min(bytes / 1024, int.MaxValue);
    }

    private static (int exit, string stdout) Reg(params string[] args)
    {
        var psi = new ProcessStartInfo("reg")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi);
        if (p is null) return (-1, "");
        var stdout = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout);
    }
}
