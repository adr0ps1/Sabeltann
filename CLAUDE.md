# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore
dotnet build
dotnet run --configuration Release
dotnet build --configuration Release --no-restore
```

Debug builds output `SabeltannDevelopment.exe` (set by `<AssemblyName>` in the csproj) so they don't conflict with a release install running alongside.

There are no automated tests; the CI `dotnet test` step runs with `continue-on-error: true`.

## Architecture

Sabeltann is a Windows IPTV player built on Avalonia 12 + LibVLCSharp. It follows MVVM using CommunityToolkit.Mvvm (source generators — `[ObservableProperty]`, `[RelayCommand]`, `partial` classes everywhere).

### App flow

1. `Program.cs` → `App.axaml.cs` bootstraps Avalonia and opens `MainWindow`.
2. `MainWindow` owns the two live objects that require disposal: `PlaybackService` (LibVLC wrapper) and `VideoView` (the native VLC surface in `Controls/VideoView.cs`). On `OnClosed` it disposes both plus `ImageService`.
3. `MainWindow` passes `PlaybackService` into `MainViewModel.SetPlayer()` after construction — the VM never creates the player itself.
4. Navigation is driven by `ContentMode` enum (`Welcome → Picker → LiveTv / Movies / Series`). All mode-dependent UI visibility derives from boolean properties on `MainViewModel` that call `[NotifyPropertyChangedFor]` on `_mode`.

### ContentMode state machine

| Mode | What's visible |
|------|---------------|
| `Welcome` | Connection page only |
| `Picker` | Content type selector (Live / Movies / Series) |
| `LiveTv` | Category sidebar + channel list + video area |
| `Movies` | `VodBrowserViewModel` renders a poster grid |
| `Series` | `SeriesBrowserViewModel` renders grouped series |

### Key service responsibilities

- **`M3UParser`** — downloads or reads an `.m3u`/`.m3u8` file and returns a `M3UPlaylist` with `List<Channel>`.
- **`XtreamService`** — authenticates with Xtream Codes API and fetches live/VOD/series stream lists. Stream URLs are constructed from the Xtream base URL + credentials.
- **`ChannelClassifier`** — stateless, heuristic classifier that assigns `ChannelType` (LiveTv / Movie / Series) to each `Channel` using regex patterns on the channel name and URL extension. Classification priority: season/episode markers → year in name → VOD file extension → EPG tvg-id → HLS URL → group name.
- **`ChannelCacheService`** — persists classified channel lists to `%LocalAppData%\Sabeltann\cache\` as JSON. Cache key is SHA-256 of the source URL/path (first 8 bytes as hex, prefixed `v2_`). For files, the key includes `LastWriteTimeUtc` so stale caches are skipped automatically.
- **`SettingsService`** — reads/writes `%LocalAppData%\Sabeltann\settings.json`. Stores last source (type + url/file/xtream creds), last category, last channel URL, favorites list, and default volume.
- **`ImageService`** — async channel logo downloader with an in-memory cache; `Shutdown()` must be called on exit.
- **`LogService`** — static, writes JSON lines to `logs/` in the app directory.
- **`PlaybackService`** — wraps `LibVLC` + `MediaPlayer`. Falls back to launching an installed external VLC if embedded playback fails. Exposes `Error`, `Buffering`, `PlayingStarted`, `Stopped` events that `MainViewModel.SetPlayer()` wires up.

### Playlist loading flow

`MainViewModel` loads channels in two phases to avoid blocking the UI:

1. **Phase 1** (`LoadM3UFromUrlAsync` / `LoadM3UFromFileAsync` / `LoginXtreamAsync`): downloads/authenticates, stores the result as `_pendingPlaylist` or `_pendingXtreamInfo`, transitions to `ContentMode.Picker`.
2. **Phase 2** (`ShowPlaylistContentAsync`): triggered when the user selects a content type from `ContentPicker`. Classifies channels, splits into `_liveChannels` / `_movieChannels` / `_seriesChannels`, begins async logo loading, and restores the last selected category and channel from settings.

### XAML / binding notes

- `AvaloniaUseCompiledBindingsByDefault` is `false` — all bindings use reflection. Compiled bindings can be enabled per-file with `x:CompileBindings="True"` if needed.
- `Setup/` directory is excluded from compilation (`<Compile Remove="Setup\**\*.cs" />`). The WiX installer sources live there.
- VLC native DLLs are copied post-build by the `CopyVlcNative` MSBuild target from the `VideoLAN.LibVLC.Windows` NuGet package.
- The Avalonia diagnostics package (`AvaloniaUI.DiagnosticsSupport`) is only included in Debug builds.

## Conventions

- File-scoped namespaces (`namespace Sabeltann;`).
- Nullable reference types enabled — avoid `!` suppression unless reasoning is obvious.
- `MainViewModel` is the single source of truth for all UI state. Views communicate back via events (e.g. `ConnectionPage.LoadM3UUrlRequested`) wired in `MainWindow`'s constructor, not via bindings to code-behind state.
