using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Sabeltann.Services;

public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/adr0ps1/Sabeltann";

    private readonly UpdateManager _manager;
    private UpdateInfo? _pending;

    public UpdateService()
    {
        _manager = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    }

    public bool IsInstalled => _manager.IsInstalled;

    public event Action<string>? UpdateReady;

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