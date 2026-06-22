using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Sabeltann.Services;

public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/adr0ps1/Sabeltann";

    private UpdateManager _manager;
    private UpdateInfo? _pending;

    public UpdateService()
    {
        _manager = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    }

    public bool IsInstalled => _manager.IsInstalled;

    public string? CurrentVersion => _manager.IsInstalled ? _manager.CurrentVersion?.ToString() : null;

    public bool IsUpdateAvailable { get; private set; }

    public string? ReleaseNotes { get; private set; }

    public bool IncludePrerelease { get; set; }

    public event Action<string>? UpdateReady;

    public async Task CheckAndDownloadAsync(IProgress<double>? progress = null)
    {
        if (!_manager.IsInstalled)
            return;

        try
        {
            // Reconstruct manager if prerelease flag differs from default
            _manager = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: IncludePrerelease));

            var update = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            ReleaseNotes = update.TargetFullRelease.NotesMarkdown;

            LogService.Info("Update available", new { version = update.TargetFullRelease.Version.ToString() });

            Action<int>? onProgress = progress is not null
                ? pct => progress.Report(pct / 100.0)
                : null;

            await _manager.DownloadUpdatesAsync(update, onProgress).ConfigureAwait(false);

            _pending = update;
            IsUpdateAvailable = true;
            UpdateReady?.Invoke(update.TargetFullRelease.Version.ToString());
            LogService.Info("Update downloaded, will install on exit");
        }
        catch (Exception ex)
        {
            LogService.Warn("Update check failed", new { type = ex.GetType().Name, message = ex.Message });
        }
    }

    public void ApplyPendingOnExit(bool restart = false)
    {
        if (_pending is null || !_manager.IsInstalled)
            return;

        try
        {
            _manager.WaitExitThenApplyUpdates(_pending, silent: true, restart: restart);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to schedule update on exit", new { type = ex.GetType().Name, message = ex.Message });
        }
    }
}