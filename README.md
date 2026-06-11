# ☠ Sabeltann

🏴‍☠️ **IPTV Player for Windows** — Avalonia 12 + LibVLCSharp

## ✨ Features

| What | How |
|------|-----|
| 📺 Playlists | M3U URL, `.m3u`/`.m3u8` files, Xtream Codes API |
| 🎬 Playback | LibVLC with D3D11VA hardware decoding |
| 🔍 Search | Type to filter channels |
| ⭐ Favorites | Star channels, persisted across restarts |
| 📂 Categories | Grouped by `group-title` |
| 📟 Debug (D) | Bitrate, packets, lost frames, corrupted data |
| 🖥️ Fullscreen (F) | Transport bar auto-hides on hover |
| 🎞️ Subtitles (CC) | Toggle VOD subtitle tracks |
| 📝 Logging | JSON logs in `logs/` folder |
| 🐛 Error tracking | Sentry |

## ⌨️ Shortcuts

`F` — Fullscreen · `Esc` — Exit fullscreen · `D` — Debug overlay

## 🛠️ Build

```bash
dotnet restore
dotnet run --configuration Release
```

Debug builds → `SabeltannDevelopment.exe` (won't clash with a running release).

## 📦 Installer (WiXSharp)

```bash
dotnet tool install --global WiXSharp.Tool
dotnet publish -c Release -r win-x64 --self-contained true
wixsharp build --project Setup\Setup.wixsharp -c Release --out installer
```

## 🚀 Release

[release-please](https://github.com/googleapis/release-please) auto-versions on merge to `main`. The release workflow builds, creates the MSI, attests with sigstore, and uploads to GitHub Releases.

## 🧱 Stack

`.NET 10` · `Avalonia 12` · `LibVLCSharp 3.9.7` · `CommunityToolkit.Mvvm 8.4.2` · `Sentry 5.x` · `Svg.Skia 3.x` · `WiXSharp`

## 📄 License

GPL-3.0 — see [LICENSE](LICENSE).
