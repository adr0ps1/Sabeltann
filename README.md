# Sabeltann

IPTV player for Windows, built with Avalonia 12 and LibVLCSharp.

## Features

- Load playlists from M3U URLs or local `.m3u`/`.m3u8` files
- Xtream Codes API login (username + password + server URL)
- Live TV playback with hardware-accelerated decoding (D3D11VA)
- Channel categories with search and favorites
- Subtitle toggle for VOD content
- Debug overlay (press D) with real-time stream stats (bitrate, packets, lost frames, corrupted data)
- Fullscreen mode (F / Esc) with auto-hiding transport bar on mouse hover
- Settings persisted to `%APPDATA%/Sabeltann/settings.json`
- JSON logging to `logs/` directory next to the executable
- Error tracking via Sentry

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| F | Toggle fullscreen |
| Esc | Exit fullscreen |
| D | Toggle debug overlay |

## Build & Run

```bash
dotnet restore
dotnet build --configuration Release
dotnet run --configuration Release
```

Debug builds produce `SabeltannDevelopment.exe` to avoid conflicting with a running release instance.

## Installer

WiXSharp produces an MSI installer. From the repo root:

```bash
dotnet tool install --global WiXSharp.Tool
dotnet publish --configuration Release --runtime win-x64 --self-contained true
wixsharp build --setup Setup\Setup.cs --out installer
```

## Release Process

This project uses [release-please](https://github.com/googleapis/release-please) for automated versioning and changelog generation. On merge to `main`:

1. release-please creates/updates a release PR
2. Merging the release PR tags a new version and triggers the release workflow
3. The workflow builds, creates the MSI installer, signs with sigstore, and uploads to the GitHub release

## Tech Stack

- **.NET 10** / **C#** — desktop application framework
- **Avalonia 12** — cross-platform UI
- **LibVLCSharp 3.9.7** — video playback
- **VideoLAN.LibVLC.Windows 3.0.21** — VLC native binaries
- **CommunityToolkit.Mvvm 8.4.2** — MVVM infrastructure
- **Sentry 5.x** — error monitoring
- **Svg.Skia 3.x** — icon rendering
- **WiXSharp** — MSI installer

## License

This project is licensed under the GPL-3.0 License - see the LICENSE file for details.
