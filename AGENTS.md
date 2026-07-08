# Sabeltann

IPTV player for Windows — Avalonia 12 + LibVLCSharp, .NET 10

## Build

```bash
dotnet restore
dotnet build
dotnet run --configuration Release
```

Debug builds produce `SabeltannDevelopment.exe` so they don't conflict with a running release install.

## Architecture

| Layer | Location |
|---|---|
| Entry point | `Program.cs` |
| App bootstrap | `App.axaml.cs` |
| Main window | `MainWindow.axaml` + `MainWindow.axaml.cs` |
| ViewModels | `ViewModels/` (MainViewModel, CategoryViewModel, ChannelListItemViewModel, DebugStatsViewModel) |
| Models | `Models/` (Channel, M3UPlaylist, VodModels, XtreamConnectionInfo) |
| Services | `Services/` (PlaybackService, M3UParser, XtreamService, ChannelGrouper, ChannelClassifier, SettingsService, LogService, ImageService, ChannelCacheService) |
| Views | `Views/` (ConnectionPage, ContentPicker, InputDialog, LoginWindow, SettingsWindow) |
| Controls | `Controls/` (VideoView.cs — VLC video host) |
| Converters | `Converters/` (BoolToColorConverter) |
| Assets | `Assets/` (logo.png, Sabeltann.ico) |
| Setup/WiX | `Setup/` (build.ps1, Setup.wxs, Components.wxs) |

## MVVM pattern

- Uses CommunityToolkit.Mvvm 8.4.2 (source generators for `[ObservableProperty]`, `[RelayCommand]`)
- Views bind to ViewModels via `DataContext`
- `MainViewModel` owns the full app state — channels, favorites, search, playback control

## Key patterns

- `PlaybackService` wraps LibVLCSharp — owns the VLC `MediaPlayer` and `LibVLC` instance
- `M3UParser` parses `.m3u`/`.m3u8` files into `List<Channel>`
- `XtreamService` handles Xtream Codes API login and stream URL generation
- `ChannelGrouper` extracts show names from episode titles (used by series grouping)
- `ChannelClassifier` classifies channels into LiveTv/Movie/Series by name, URL, and group heuristics
- `SettingsService` persists user preferences (last server, favorites, volume)
- `LogService` writes JSON log entries to `logs/` directory

## Code conventions

- .csproj compiles `Setup/` sources with `<Compile Remove="Setup\**\*.cs" />`
- VLC native DLLs are copied post-build via `CopyVlcNative` target
- No nullable warnings — project has `<Nullable>enable</Nullable>`
- File-scoped namespaces (`namespace Sabeltann;`)
- `favorites.json` and `settings.json` stored in `%LocalAppData%\Sabeltann\`

## Testing

After changes, output a numbered list of what to test manually. Only include changes made between the last run and the next start — do not list pre-existing behavior.
