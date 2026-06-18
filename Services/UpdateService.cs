using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Sabeltann.Services;

/// <summary>
/// Auto-update via Velopack, sourced from this project's GitHub releases.
///
/// Updates are checked and downloaded silently in the background while the app
/// runs. A staged update is applied on exit (see <see cref="ApplyPendingOnExit"/>)
/// so the user is never interrupted mid-session. When the app is not running as
/// an installed Velopack build (e.g. dev runs or the raw publish folder),
/// <see cref="UpdateManager.IsInstalled"/> is false and every method is a no-op.
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/adr0ps1/Sabeltann";

    private readonly UpdateManager _manager;
    private UpdateInfo? _pending;

    public UpdateService()
    {
        _manager = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    }

    /// <summary>True only when launched from a Velopack install.</summary>
    public bool IsInstalled => _manager.IsInstalled;

    /// <summary>Raised on a staged update, carrying the new version string.</summary>
    public event Action<string>? UpdateReady;

    /// <summary>
    /// Check GitHub for a newer release and, if found, download it. Safe to call
    /// fire-and-forget; failures are logged and swallowed so a missing network or
    /// release never disrupts startup.
    /// </summary>
    public async Task CheckAndDownloadAsync()
    {
        if (!_manager.IsInstalled)
            return;

        try
        {
            var update = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            LogService.Info("Update available", new { version = update.TargetFullRelease.Version.ToString() });
            await _manager.DownloadUpdatesAsync(update).ConfigureAwait(false);

            _pending = update;
            UpdateReady?.Invoke(update.TargetFullRelease.Version.ToString());
            LogService.Info("Update downloaded, will install on exit");
        }
        catch (Exception ex)
        {
            LogService.Warn("Update check failed", new { type = ex.GetType().Name, message = ex.Message });
        }
    }

    /// <summary>
    /// Apply a downloaded update once this process exits, without restarting.
    /// Call during shutdown; no-op if nothing is staged.
    /// </summary>
    public void ApplyPendingOnExit()
    {
        if (_pending is null || !_manager.IsInstalled)
            return;

        try
        {
            _manager.WaitExitThenApplyUpdates(_pending, silent: true, restart: false);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to schedule update on exit", new { type = ex.GetType().Name, message = ex.Message });
        }
    }
}
