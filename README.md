<div align="center">

<img src="Assets/logo.png" width="120" height="120" />

# Sabeltann!

**IPTV player for Windows and macOS**

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
| Auto-update | Velopack — checks GitHub Releases on launch, installs on exit |

## macOS

Paste this into Terminal:

```bash
curl -fL -o ~/Downloads/Sabeltann.pkg \
  https://github.com/adr0ps1/Sabeltann/releases/latest/download/Sabeltann-osx.pkg
open ~/Downloads/Sabeltann.pkg
```

The installer opens, you pick `/Applications` or `~/Applications`, and the app launches
when it finishes. No warnings to click through.

That is not a trick to feel clever about: macOS quarantines downloads because the
*downloading app* tags them, and `curl` does not. Sabeltann is neither signed nor
notarized (no Apple Developer ID), so nobody has checked this build for malware and
macOS cannot verify who built it. The GitHub release carries Sigstore build provenance
if you want to confirm it came from this repo's CI.

<details>
<summary>Downloading with a browser instead</summary>

Safari and Chrome do apply the quarantine flag, so Gatekeeper will block the `.pkg`.
Clear it and open:

```bash
xattr -d com.apple.quarantine ~/Downloads/Sabeltann-*.pkg
open ~/Downloads/Sabeltann-*.pkg
```

Without Terminal: right-click the `.pkg` → **Open** → **Open**. On macOS 15 and later
that route may be closed, in which case use System Settings → Privacy & Security →
**Open Anyway**. If the installed app is blocked too, `xattr -cr /Applications/Sabeltann.app`.

</details>

**Apple Silicon** runs the app under Rosetta 2 — the build is Intel x86-64, because
VideoLAN publishes no arm64 libvlc NuGet package
([libvlc-nuget#17](https://code.videolan.org/videolan/libvlc-nuget/-/issues/17)). macOS
normally offers to install Rosetta on first launch; if it does not,
`softwareupdate --install-rosetta --agree-to-license`.

**Recording** needs `ffmpeg` on `PATH` (`brew install ffmpeg`) — unlike Windows the app
does not download one. Rounded window corners are Windows-only.

## Keyboard shortcuts

`F` — fullscreen · `Esc` — exit fullscreen · `D` — debug overlay

## Build from source

```bash
dotnet restore
dotnet run --configuration Release
```

Debug builds produce `SabeltannDevelopment.exe` so they don't conflict with a running release install.

## Release process

Merging to `main` triggers [release-please](https://github.com/googleapis/release-please) to auto-version. The release workflow publishes the app, packs a [Velopack](https://velopack.io) release (`Setup.exe`, portable zip, and full/delta update packages), attests with Sigstore, and uploads everything to the GitHub release for the tag. The in-app updater reads those release assets to deliver auto-updates.

A second job then does the same on `macos-latest` for `osx-x64`, producing a `.pkg`. Velopack keeps one release manifest per platform (`releases.osx.json` next to `releases.win.json`), so the in-app updater picks the right channel with no code change. It runs after the Windows job rather than beside it, because both merge assets into the same GitHub release. Finally it uploads a copy of the installer as `Sabeltann-osx.pkg` — Velopack's own filename carries the version, and the fixed name is what makes the `releases/latest/download/` URL in the install instructions permanent.

## Tech stack

- .NET 10
- Avalonia 12
- LibVLCSharp 3.9.7
- CommunityToolkit.Mvvm 8.4.2
- Svg.Skia 3.x
- Velopack 1.2 (installer + auto-update)

**Roles:**
- Author — [@adr0ps1](https://github.com/adr0ps1)
- Reviewer — [@adr0ps1](https://github.com/adr0ps1)
- Approver — [@adr0ps1](https://github.com/adr0ps1)

**Privacy:** Sabeltann does not collect or transmit personal data. Logs are stored locally. See [Privacy Policy](PRIVACY.md).

**Disclaimer:** Sabeltann is a media player — it does not host, provide, or endorse any IPTV streams or content. All playlists, URLs, and sources are supplied entirely by the user. You are solely responsible for ensuring the content you access complies with applicable laws in your jurisdiction. Unauthorized streaming of copyrighted material may be illegal in your country.

## License
This project is licensed under the **GNU General Public License v3.0** — a copyleft license that requires derivative works to be distributed under the same license.

See [LICENSE](LICENSE) for the full text.
