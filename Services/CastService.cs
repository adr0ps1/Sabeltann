using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sharpcaster;
using Sharpcaster.Models;
using Sharpcaster.Models.Media;

namespace Sabeltann.Services;

/// <summary>
/// Native Google Cast sender (SharpCaster). The Chromecast fetches and plays the stream URL itself,
/// so audio/subtitle tracks switch live via <see cref="SetActiveTracksAsync"/> with no pipeline
/// rebuild — unlike the libvlc renderer path in <see cref="PlaybackService"/>. Only works for
/// Chromecast-playable streams (HLS / H.264 / AAC); callers fall back to the libvlc cast for the rest.
/// </summary>
public class CastService : IAsyncDisposable
{
    // Google's Default Media Receiver — plays a plain URL, incl. HLS, with track support.
    private const string DefaultMediaReceiver = "CC1AD845";

    private ChromecastClient? _client;

    public bool IsConnected => _client is not null;

    /// <summary>mDNS discovery of Cast receivers. NOTE: same libmicrodns-vs-Tailscale caveat may apply
    /// as the libvlc path — see chromecast-tailscale-mdns. Zeroconf-based here, so verify separately.</summary>
    public async Task<IReadOnlyList<ChromecastReceiver>> FindDevicesAsync(TimeSpan timeout)
    {
        var locator = new ChromecastLocator();
        var devices = await locator.FindReceiversAsync(timeout);
        return devices.ToList();
    }

    /// <summary>Connects, launches the media receiver, and loads the stream. Returns the initial status
    /// (carries the receiver-detected track list for HLS once playback starts).</summary>
    public async Task<MediaStatus?> CastAsync(
        ChromecastReceiver device, string url, string contentType, StreamType streamType,
        Track[]? tracks = null, int[]? activeTrackIds = null, string? title = null)
    {
        await DisconnectAsync();
        _client = new ChromecastClient();
        await _client.ConnectChromecast(device);
        await _client.LaunchApplicationAsync(DefaultMediaReceiver);

        var media = new Media
        {
            ContentUrl = url,
            ContentType = contentType,
            StreamType = streamType,
            Tracks = tracks,
            Metadata = title is null ? null : new MediaMetadata { Title = title },
        };
        return await _client.MediaChannel.LoadAsync(media, true, activeTrackIds);
    }

    /// <summary>Switches the active audio/subtitle track(s) on the running cast — no reload.</summary>
    public Task<MediaStatus?> SetActiveTracksAsync(int[] activeTrackIds)
        => _client?.MediaChannel.EditTracksAsync(activeTrackIds) ?? NullStatus();

    public Task<MediaStatus?> PlayAsync() => _client?.MediaChannel.PlayAsync() ?? NullStatus();
    public Task<MediaStatus?> PauseAsync() => _client?.MediaChannel.PauseAsync() ?? NullStatus();
    public Task<MediaStatus?> SeekAsync(double seconds) => _client?.MediaChannel.SeekAsync(seconds) ?? NullStatus();
    public Task<MediaStatus?> GetStatusAsync() => _client?.MediaChannel.GetMediaStatusAsync() ?? NullStatus();

    private static Task<MediaStatus?> NullStatus() => Task.FromResult<MediaStatus?>(null);

    public async Task DisconnectAsync()
    {
        if (_client is null) return;
        try { await _client.DisconnectAsync(); }
        catch (Exception e) { LogService.Warn("Cast disconnect failed", new { error = e.Message }); }
        _client = null;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
