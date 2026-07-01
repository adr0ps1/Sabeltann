# Chromecast feature — resume handoff (branch `feat/chromecast-61`)

> Working doc for resuming the Chromecast work. NOT a product doc. Delete before merge.
> Last updated: 2026-06-30.

## TL;DR of where we are

libvlc-as-renderer casting **works** for Live TV and Movies, but **track switching while casting
is fundamentally limited by libvlc** (every change rebuilds the cast pipeline = visible restart, and
subtitles only show if libvlc transcodes the video). User wants **smooth, no-restart, remote-controllable
track switching like other IPTV apps**. That requires abandoning libvlc casting in favor of the
**native Google Cast protocol**. **Decision made: build native-cast as primary + libvlc-transcode as fallback.**
Next step is the design pass for that.

## Why libvlc casting can't do smooth track switching (researched, settled)

- libvlc's chromecast renderer muxes/transcodes ONE program and streams it. No live track API → any
  audio/subtitle change tears down and rebuilds the `sout` chain (the quit-to-home-then-reconnect).
- Subtitles only appear when libvlc **transcodes** the video (it adds `soverlay` to burn them in,
  per `modules/stream_out/chromecast/cast.cpp`). For Chromecast-native codecs it **remuxes** instead →
  no `soverlay` → no subtitles. This is open VLC issue #25695.
- Confirmed in source: `cast.cpp` forces transcode + `soverlay` when an SPU ES is selected; option
  `sout-chromecast-conversion-quality` controls quality. Burn-in works but = full re-encode (CPU/latency/quality).

## The target architecture (what other apps do)

Native Google Cast (`EditTracksInfo` / `setActiveTrackIds`):
1. Sender hands the Chromecast the **stream URL directly** (HLS/m3u8) + a **tracks manifest**.
2. Chromecast plays it itself (built-in/Shaka player), all tracks loaded up front.
3. Track switch = one `EDIT_TRACKS` message → receiver toggles active track **client-side, no reload**.
   Subtitles render natively; TV remote can control them.

C# libs: **SharpCaster** (Tapanila, .NET 9, NuGet, mDNS built in) or **GoogleCast** (kakone). SharpCaster
README does NOT document `EditTracksInfo` — may need to send the raw `EDIT_TRACKS` Cast message ourselves
(protocol supports it; verify in lib source first).

### Hard constraints (real, design around them)
- Native cast only works for **Chromecast-playable** streams: HLS/H.264/AAC (most IPTV live + much VOD).
  Raw MPEG-TS / unsupported codecs (HEVC on old devices) → **fall back to libvlc-transcode cast**.
- Subtitles must be Cast-compatible: **WebVTT renditions inside the HLS manifest** switch smoothly.
  **Embedded subs in a direct `.mkv`/`.mp4`** (the VOD case tested) generally won't show natively without
  a sidecar VTT. That case stays limited even with native cast.

## Proposed phased plan (native-cast + libvlc fallback)

1. **Add SharpCaster; `CastService`** — discovery + connect + `LoadAsync(HLS url)`. Verify a live HLS
   channel plays on the TV via native cast (no libvlc). Stop/replace the current libvlc cast path for HLS.
2. **Track query + switch** — read tracks from Cast `MediaStatus`; populate the existing
   `AudioTrackItems`/`SubtitleTrackItems`; switch via `EDIT_TRACKS` (`setActiveTrackIds`). Verify smooth
   audio + subtitle switch with NO restart.
3. **Fallback detection** — route non-Chromecast-native streams to the existing libvlc-transcode cast.
   Preserve VOD resume position across both paths.
4. **UI/state + cleanup** — overlay, cast icon, transport (play/pause/seek/stop) routed to whichever cast
   path is active; stop-casting resumes libvlc local at position. Remove the diagnostic instrumentation
   listed below.

### Open design questions
- Does SharpCaster expose `EditTracksInfo`, or do we hand-roll the `EDIT_TRACKS` message? (read its source)
- Does native Cast discovery ALSO hit the Tailscale-adapter mDNS bug (below)? Almost certainly yes (same mDNS).
- Cheap "is this stream Chromecast-native?" check — start with URL extension (`.m3u8` → native), refine later.
- Whether the IPTV provider's HLS even includes VTT subtitle renditions (provider-dependent).

## CRITICAL environment gotcha — Tailscale breaks discovery

libvlc's `libmicrodns` mDNS finds NO cast devices whenever a **Tailscale (or any VPN TUN) adapter is present**.
`tailscale down` is NOT enough — the **adapter must be disabled/removed**. Verified: disabling the Tailscale
adapter makes the LG TV appear instantly. This is environmental, not our code (a code-free PowerShell mDNS
probe showed the same). To test casting you MUST disable the Tailscale adapter:
```
Get-Service *tailscale* | Stop-Service -Force; Disable-NetAdapter -Name Tailscale -Confirm:$false
# ...test...
Start-Service Tailscale; Enable-NetAdapter -Name Tailscale -Confirm:$false   # then: tailscale up
```
Documented in `Services/PlaybackService.cs` at `StartCastDiscovery`, and memory `chromecast-tailscale-mdns`.

## Current uncommitted state on the branch

Modified: `MainWindow.axaml`, `MainWindow.axaml.cs`, `Services/PlaybackService.cs`, `ViewModels/MainViewModel.cs`.

Working & done:
- Cast discovery (`RendererDiscoverer`/microdns), cast menu, casting overlay, cast icon, stop-casting.
- `CanRenderVideo` listing filter removed (TV reports it true anyway).
- Fullscreen restores Maximized-vs-Normal; volume popup anchors to its button.
- Removed dead `Stop` command; transport Stop → `StopPlaybackCommand`.
- Watchdog confirmation reworked around `_playbackConfirmed` (frame for local, `InputBytes` for cast).
- Track-switch-while-casting fix (libvlc path): `SelectAudio`/`SelectSubtitle` branch on `IsCasting`,
  bake `:audio-track-id=`/`:sub-track-id=` into media options and restart via `RestartCurrent()`,
  preserving VOD position. `BeginCastWindow()` resets the watchdog window on cast (re)start.
  → AUDIO switching should work; SUBTITLE switching does NOT (remux skips soverlay — the whole reason
  for the native-cast pivot).

### Diagnostic instrumentation still in the code — REMOVE/finalize during Phase 4
- `Services/PlaybackService.cs`: `PlayerState` property (diagnostic), verbose logging in `StartCastDiscovery`
  / `OnRendererAdded` (`renderer modules`, `started`, `item added`) — keep the no-module warn, trim the rest.
- `ViewModels/MainViewModel.cs` position timer: `"Cast progress"` per-tick log (DIAG); cast watchdog is in
  **diagnostic mode — it does NOT kill a stuck cast, only logs** `"Cast watchdog deadline (not killing — diag)"`.
  A real cast liveness signal still needs choosing (likely moot once native-cast lands).

## PROGRESS (2026-07-01)

- Added `SharpCaster` 3.0.0 NuGet (pulls Zeroconf mDNS, Google.Protobuf, System.Reactive). In `Sabeltann.csproj`.
- **Created `Services/CastService.cs`** (namespace `Sabeltann.Services`) — compiles clean. Native-cast wrapper:
  discovery, connect, launch DMR, load, live track switch, transport. This is the Phase-1 service layer, DONE.

### SharpCaster 3.0.0 API actually used (verified against installed lib)
- Discovery: `new ChromecastLocator().FindReceiversAsync(TimeSpan? timeout)` → `IEnumerable<ChromecastReceiver>`.
- Connect: `var c = new ChromecastClient(); await c.ConnectChromecast(receiver); await c.LaunchApplicationAsync("CC1AD845");`
- Load: `await c.MediaChannel.LoadAsync(Media media, bool autoPlay = true, int[]? activeTrackIds = null)` → `MediaStatus?`.
- **Live track switch (the whole point):** `await c.MediaChannel.EditTracksAsync(int[] activeTrackIds, TextTrackStyle?, string? language, object?)` → `MediaStatus?`. No reload.
- Transport: `MediaChannel.PlayAsync/PauseAsync/StopAsync/SeekAsync(double sec)/GetMediaStatusAsync/SetVolumeAsync/SetMuteAsync`.
- `Media` props: `ContentUrl`, `ContentType`, `StreamType` (enum in `Sharpcaster.Models.Media`), `Tracks (Track[]?)`, `Metadata (MediaMetadata{Title})`, `Duration`.
- `Track` props: `TrackId (int)`, `Type (TrackType: audio/video/text)`, `Subtype (TextTrackType?)`, `Language`, `Name`, `TrackContentId`, `TrackContentType`.
- Disconnect: `await c.DisconnectAsync()` (CastService wraps as `DisconnectAsync()` / `DisposeAsync`).

### Remaining integration (Phase 1 finish + Phase 2) — NOT yet wired
1. `MainWindow` owns a `CastService` instance; dispose on `OnClosed` (it's `IAsyncDisposable`). Pass into VM (like `SetPlayer`).
2. **Discovery source:** decide whether to replace libvlc's `RendererDiscoverer` menu with `CastService.FindDevicesAsync`,
   or keep both. Simplest: one native discovery feeding the cast menu; keep libvlc renderers only for the fallback path.
3. **Cast a stream:** in `CastTo`, if the current URL is Chromecast-native (start heuristic: `.m3u8` → HLS,
   `ContentType="application/x-mpegurl"`, `StreamType.Live` for live / `Buffered` for VOD), STOP libvlc local playback and
   `await _cast.CastAsync(device, url, contentType, streamType, title: name)`. Else fall back to the existing libvlc renderer cast.
4. **Populate track menus from the cast:** after load, read tracks from `MediaStatus` (poll `GetStatusAsync` — HLS tracks
   appear once the receiver parses the manifest). Map into `AudioTrackItems`/`SubtitleTrackItems` (TrackId as id).
5. **Switch:** when native-casting, `SelectAudio`/`SelectSubtitle` call `_cast.SetActiveTracksAsync(new[]{...})` (combine the
   chosen audio + subtitle ids) instead of the libvlc `:audio-track-id` restart. NO restart, smooth.
6. **Transport while native-casting:** route play/pause/seek/stop to `CastService`; `StopCasting` → disconnect + resume libvlc local at position.
7. Overlay/icon/`IsCasting` state must reflect whichever cast path is active (add a `CastMode { None, Native, Libvlc }`).

## Build/test
- `dotnet build` — clean (0 errors). Debug exe: `bin/Debug/net10.0/SabeltannDevelopment.exe`.
- To exercise cast discovery: disable Tailscale adapter (above), run the exe ~15s, then read
  `bin/Debug/net10.0/logs/sabeltann-<date>.jsonl` for `Cast discovery` / `Cast progress` lines.
