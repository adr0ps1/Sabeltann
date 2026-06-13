<div align="center">

<img src="Assets/pirate.svg" width="120" height="120" />

# Sabeltann

**IPTV player for Windows** — because watching TV shouldn't feel like doing your taxes.

Built with Avalonia 12 and LibVLCSharp. Plays pretty much anything you throw at it — M3U playlists, Xtream Codes, local files, you name it.

</div>

## What it does

| Thing | How |
|---|---|
| Playlists | M3U URLs, `.m3u`/`.m3u8` files, Xtream Codes API |
| Playback | LibVLC with D3D11VA hardware decoding |
| Search | Just start typing, it filters channels live |
| Favorites | Star a channel and it'll be there next time |
| Categories | Channels grouped by `group-title` |
| Debug overlay (D) | Bitrate, packets, lost frames, corrupted data |
| Fullscreen (F) | Transport bar hides itself when you don't need it |
| Subtitles (CC) | Toggle VOD subtitle tracks |
| Logging | JSON logs in `logs/`, handy for troubleshooting |
| Error tracking | Sentry, if you set it up |

## Keyboard shortcuts

`F` — fullscreen · `Esc` — get out of fullscreen · `D` — debug overlay

## Building from source

```bash
dotnet restore
dotnet run --configuration Release
```

Debug builds produce `SabeltannDevelopment.exe` so they don't step on a running release install.

## Making an installer

```bash
dotnet publish -c Release -r win-x64 --self-contained true
$env:PRODUCT_VERSION = "v1.0.0"
pwsh Setup\build.ps1
# You'll find your MSI at installer\Sabeltann-1.0.0.msi
```

## How releases work

Merge to `main`, and [release-please](https://github.com/googleapis/release-please) handles the version bumping automatically. The release workflow builds the app, packages the MSI, attests everything with Sigstore, and uploads it all to GitHub Releases. Pretty hands-off.

## Tech stack

`.NET 10` · `Avalonia 12` · `LibVLCSharp 3.9.7` · `CommunityToolkit.Mvvm 8.4.2` · `Sentry 5.x` · `Svg.Skia 3.x` · `WiX v4`

## Code signing

Big thanks to [SignPath Foundation](https://signpath.org/) for providing free Windows code signing for open source projects. The certificate is issued to SignPath Foundation, not to me, but it's still a proper Authenticode signature — SmartScreen will actually trust it.

Roles for signing:

- **Author** — [@adr0ps1](https://github.com/adr0ps1) (me, the one writing the code)
- **Reviewer** — also me
- **Approver** — yep, still me

Only tagged releases from `main` get signed. No random builds floating around with signatures.

**Privacy:** Sabeltann doesn't phone home. Logs stay in your `logs/` folder. Sentry error tracking is opt-in only. See [Privacy Policy](PRIVACY.md).

## License

GPL-3.0 — see [LICENSE](LICENSE). In short: do what you want with the code, but if you modify and distribute it, that has to stay open too.
