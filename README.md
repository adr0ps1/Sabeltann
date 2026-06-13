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

## 📦 Installer (WiX Toolset v4)

```bash
dotnet publish -c Release -r win-x64 --self-contained true
$env:PRODUCT_VERSION = "v1.0.0"
pwsh Setup\build.ps1
# MSI → installer\Sabeltann-1.0.0.msi
```

## 🚀 Release

[release-please](https://github.com/googleapis/release-please) auto-versions on merge to `main`. The release workflow builds, creates the MSI, attests with sigstore, and uploads to GitHub Releases.

## 🧱 Stack

`.NET 10` · `Avalonia 12` · `LibVLCSharp 3.9.7` · `CommunityToolkit.Mvvm 8.4.2` · `Sentry 5.x` · `Svg.Skia 3.x` · `WiX v4`

## 📜 Code Signing Policy

Windows code signing for Sabeltann is provided free of charge by [SignPath Foundation](https://signpath.org/), with a certificate issued to SignPath Foundation as the publisher. All release builds are signed via [SignPath.io](https://signpath.io/) in the GitHub Actions release pipeline.

**Roles:**
- **Author** — [@adr0ps1](https://github.com/adr0ps1) — trusted to modify source code
- **Reviewer** — [@adr0ps1](https://github.com/adr0ps1)
- **Approver** — [@adr0ps1](https://github.com/adr0ps1) — authorizes each signing request

Signed binaries are built from the `main` branch via the Release workflow. Only tagged releases are signed.

**Privacy:** Sabeltann does not collect or transmit any personal information. Logs are stored locally in the `logs/` folder and never sent externally unless error tracking (Sentry) is explicitly configured. For more details, see [Privacy Policy](PRIVACY.md).

## 📄 License

GPL-3.0 — see [LICENSE](LICENSE).
