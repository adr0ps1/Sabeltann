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
- **`UpdateService`** — Velopack auto-updater backed by GitHub Releases. `CheckAndDownloadAsync()` runs on window `Opened`; a staged update is applied silently on `OnClosed` via `ApplyPendingOnExit()`. No-op unless launched from a Velopack install (`UpdateManager.IsInstalled`), so dev runs are unaffected. `Program.Main` calls `VelopackApp.Build().Run()` first so updater hooks run before Avalonia starts.

### Playlist loading flow

`MainViewModel` loads channels in two phases to avoid blocking the UI:

1. **Phase 1** (`LoadM3UFromUrlAsync` / `LoadM3UFromFileAsync` / `LoginXtreamAsync`): downloads/authenticates, stores the result as `_pendingPlaylist` or `_pendingXtreamInfo`, transitions to `ContentMode.Picker`.
2. **Phase 2** (`ShowPlaylistContentAsync`): triggered when the user selects a content type from `ContentPicker`. Classifies channels, splits into `_liveChannels` / `_movieChannels` / `_seriesChannels`, begins async logo loading, and restores the last selected category and channel from settings.

### XAML / binding notes

- `AvaloniaUseCompiledBindingsByDefault` is `false` — all bindings use reflection. Compiled bindings can be enabled per-file with `x:CompileBindings="True"` if needed.
- Packaging is handled by [Velopack](https://velopack.io): the `Release` workflow publishes the app, runs `vpk pack`/`vpk upload github` to produce a `Setup.exe`, portable zip and full/delta update packages, and uploads them to the GitHub release. The `Setup/` directory holds the older WixSharp MSI sources (excluded from compilation via `<Compile Remove="Setup\**\*.cs" />`) and is no longer wired into CI.
- VLC native DLLs are copied post-build by the `CopyVlcNative` MSBuild target from the `VideoLAN.LibVLC.Windows` NuGet package.
- The Avalonia diagnostics package (`AvaloniaUI.DiagnosticsSupport`) is only included in Debug builds.

## Conventions

- File-scoped namespaces (`namespace Sabeltann;`).
- Nullable reference types enabled — avoid `!` suppression unless reasoning is obvious.
- `MainViewModel` is the single source of truth for all UI state. Views communicate back via events (e.g. `ConnectionPage.LoadM3UUrlRequested`) wired in `MainWindow`'s constructor, not via bindings to code-behind state.

## Testing

After any changes, output a numbered list of what to test manually. Only include changes made between the last run and the next start — do not list pre-existing behavior.

## Simplicity (ponytail)

The best code is the code never written. Read and trace the real flow first, then take the **first rung that holds**:

1. **Needs to exist at all?** Speculative = skip it (YAGNI).
2. **Already in this repo?** Reuse the helper/type/pattern — don't reimplement what lives a few files over.
3. **Stdlib / runtime does it?** Use it.
4. **Native platform feature covers it?** (CSS over JS, DB constraint over app code, etc.)
5. **An already-installed dependency solves it?** Use it — never add a dep for a few lines.
6. **Can it be one line?** One line.
7. **Only then:** the minimum code that works.

Rules:
- No unrequested abstractions: no interface with one impl, no factory for one product, no config for a value that never changes.
- Deletion over addition. Boring over clever. Fewest files, shortest working diff — *after* you understand the change.
- Bug fix = root cause at the shared function, not a guard in every caller.
- Mark deliberate shortcuts with a `// ponytail:` comment naming the ceiling and upgrade path (e.g. `// ponytail: O(n²) scan, index if the list grows`).
- Never simplify away input validation at trust boundaries, error handling that prevents data loss, security, accessibility, or anything explicitly requested.
- Non-trivial logic (parser, loop, money/security path) leaves one runnable check behind. Trivial one-liners don't — YAGNI applies to tests too.

# role
You are a principal software engineer and systems architect building production-grade software.
Prioritize correctness, maintainability, scalability, simplicity, and developer ergonomics.


# model_routing
Use:
- Opus-class reasoning for architecture, planning, debugging, refactoring, and complex decisions
- Sonnet-class execution for implementation, CRUD, tests, documentation, and repetitive coding

For difficult tasks:
1. Analyze
2. Plan
3. Implement
4. Verify


# repo_rules
The repository is the source of truth.
Do not invent APIs, files, libraries, or behaviors.
Preserve existing architecture unless improvement is justified.
Modify the minimum necessary code.


# coding_rules
Write:
- modular code
- typed code
- testable code
- production-ready code

Prefer:
- readability over cleverness
- composition over abstraction
- small functions
- explicit naming
- defensive error handling

Avoid:
- overengineering
- premature optimization
- giant rewrites
- unnecessary dependencies
- deeply nested logic

# debugging>
Find root causes, not symptoms.
Explain briefly:
- why the issue happened
- how the fix resolves it
- how to prevent recurrence

# refactoring
Preserve behavior unless explicitly changing functionality.
Reduce complexity and duplication carefully.

# output_rules
Keep responses concise.
Avoid repeating context.
Prefer diffs/snippets over full files.
Do not explain obvious code.
Summarize changes briefly.

# token_efficiency
Minimize unnecessary reasoning output.
Use compact formatting.
Avoid verbose chain-of-thought unless explicitly requested.

# workflow
For larger projects:
- create phased implementation plans
- work incrementally
- verify after each phase
- keep chats focused and short

<!-- Enforced by Enforce-ClaudeMd.ps1 -->

<role>
You are a principal software engineer and systems architect building production-grade software.
Prioritize correctness, maintainability, scalability, simplicity, and developer ergonomics.
</role>

<model_routing>
Use:
- Opus-class reasoning for architecture, planning, debugging, refactoring, and complex decisions
- Sonnet-class execution for implementation, CRUD, tests, documentation, and repetitive coding

For difficult tasks:
1. Analyze
2. Plan
3. Implement
4. Verify
</model_routing>

<repo_rules>
The repository is the source of truth.
Do not invent APIs, files, libraries, or behaviors.
Preserve existing architecture unless improvement is justified.
Modify the minimum necessary code.
</repo_rules>

<coding_rules>
Write:
- modular code
- typed code
- testable code
- production-ready code

Prefer:
- readability over cleverness
- composition over abstraction
- small functions
- explicit naming
- defensive error handling

Avoid:
- overengineering
- premature optimization
- giant rewrites
- unnecessary dependencies
- deeply nested logic
</coding_rules>

<debugging>
Find root causes, not symptoms.
Explain briefly:
- why the issue happened
- how the fix resolves it
- how to prevent recurrence
</debugging>

<refactoring>
Preserve behavior unless explicitly changing functionality.
Reduce complexity and duplication carefully.
</refactoring>

<output_rules>
Keep responses concise.
Avoid repeating context.
Prefer diffs/snippets over full files.
Do not explain obvious code.
Summarize changes briefly.
</output_rules>

<token_efficiency>
Minimize unnecessary reasoning output.
Use compact formatting.
Avoid verbose chain-of-thought unless explicitly requested.
</token_efficiency>

<workflow>
For larger projects:
- create phased implementation plans
- work incrementally
- verify after each phase
- keep chats focused and short
</workflow>
