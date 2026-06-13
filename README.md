<div align="center">

<img src="Assets/pirate.svg" width="120" height="120" />

# Sabeltann

**IPTV player for Windows**

Built with Avalonia 12 and LibVLCSharp. Supports M3U playlists, Xtream Codes, and local files.

</div>

## Features

| Feature | Details |
|---|---|
| Playlists | M3U URLs, `.m3u`/`.m3u8` files, Xtream Codes API |
| Playback | LibVLC with D3D11VA hardware decoding |
| Search | Type to filter channels |
| Favorites | Star a channel, persists between restarts |
| Categories | Grouped by `group-title` |
| Debug overlay (D) | Bitrate, packets, lost frames, corrupted data |
| Fullscreen (F) | Transport bar auto-hides |
| Subtitles (CC) | Toggle VOD subtitle tracks |
| Logging | JSON logs in `logs/` |
| Error tracking | Sentry (optional) |

## Keyboard shortcuts

`F` — fullscreen · `Esc` — exit fullscreen · `D` — debug overlay

## Build from source

```bash
dotnet restore
dotnet run --configuration Release
```

Debug builds produce `SabeltannDevelopment.exe` so they don't conflict with a running release install.

## Release process

Merging to `main` triggers [release-please](https://github.com/googleapis/release-please) to auto-version. The release workflow builds the app, creates the MSI, attests with Sigstore, and uploads to GitHub Releases.

## Tech stack

- .NET 10
- Avalonia 12
- LibVLCSharp 3.9.7
- CommunityToolkit.Mvvm 8.4.2
- Sentry 5.x
- Svg.Skia 3.x
- WiX v4

## Code signing

Code signing for Windows is provided by [SignPath Foundation](https://signpath.org/). The certificate is issued to SignPath Foundation. Only tagged releases from `main` are signed.

**Roles:**
- Author — [@adr0ps1](https://github.com/adr0ps1)
- Reviewer — [@adr0ps1](https://github.com/adr0ps1)
- Approver — [@adr0ps1](https://github.com/adr0ps1)

**Privacy:** Sabeltann does not collect or transmit personal data. Logs are stored locally. Sentry error tracking is opt-in. See [Privacy Policy](PRIVACY.md).

## License

GPL-3.0 — see [LICENSE](LICENSE).

**Important:** Usage of the signed installer/binaries is restricted to applications that have been code-signed through SignPath Foundation approval. Distribution of unsigned builds or use of the software before obtaining a valid code signing certificate is not permitted.
