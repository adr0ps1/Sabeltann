# Contributing to Sabeltann

Thanks for your interest in contributing! Sabeltann is a Windows IPTV player built on Avalonia 12 + LibVLCSharp, and contributions of all sizes are welcome — bug fixes, new features, and documentation improvements alike.

## Prerequisites

- **Windows 10/11** (the app is Windows-only due to LibVLC native DLLs)
- **.NET 9 SDK** — [download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IDE**: Visual Studio 2022 (Community or higher) or JetBrains Rider

## Fork → Branch → PR workflow

1. Fork the repo and clone your fork locally.
2. Create a branch from `main` using the naming conventions below.
3. Make your changes, build, and test manually.
4. Open a pull request against `main` with a clear description.

### Branch naming

| Type | Pattern | Example |
|------|---------|---------|
| Bug fix | `fix/short-description` | `fix/volume-popup-flicker` |
| New feature | `feat/short-description` | `feat/series-continue-watching` |
| Chore / tooling | `chore/short-description` | `chore/update-avalonia` |

### Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/) style:

```
fix: volume popup no longer flickers on mouse leave
feat: add continue-watching marker for series episodes
chore: bump Avalonia to 11.3.1
```

Keep the subject line under 72 characters. Add a body if the change needs context.

## Building

```bash
# Restore NuGet packages
dotnet restore

# Debug build (outputs SabeltannDevelopment.exe — safe to run alongside a release install)
dotnet build

# Release build
dotnet build --configuration Release --no-restore

# Run in release mode
dotnet run --configuration Release
```

> **Debug vs Release**: Debug builds produce `SabeltannDevelopment.exe` (controlled by `<AssemblyName>` in the csproj) so they don't clash with an installed release copy running at the same time.

## Testing

There are no automated tests. After making changes, please manually test the scenarios affected by your change. When you open a PR, describe what you tested and how — the PR template has a section for this.

Common things to verify depending on your change:

- Playback starts, pauses, and stops correctly
- Volume control and mute behave as expected
- Keyboard shortcuts work and don't fire when typing in the search box
- Navigation between Welcome → Picker → Live/Movies/Series works
- Settings persist across restarts (last channel, volume, favorites)
- No sensitive data (credentials, API keys) is logged or persisted in unexpected places

## Architecture overview

The codebase follows MVVM with `CommunityToolkit.Mvvm` source generators. Key points:

- `MainViewModel` is the single source of truth for all UI state.
- Views communicate back via events wired in `MainWindow`'s constructor — not via code-behind state.
- `PlaybackService` wraps LibVLC and is owned by `MainWindow`, injected into the VM via `SetPlayer()`.
- Navigation is driven by the `ContentMode` enum.

See [CLAUDE.md](./CLAUDE.md) for a full architectural breakdown if you want to go deeper before contributing.

## PR checklist

Before submitting, make sure your PR:

- [ ] Builds without errors (`dotnet build`)
- [ ] Has been manually tested against the changed functionality
- [ ] Follows branch naming and commit message conventions
- [ ] Does not commit secrets, credentials, or API keys
- [ ] Includes a clear description of what changed and why
- [ ] References any related issue (e.g. `Closes #42`)

## Questions?

Open a [GitHub Discussion](https://github.com/adr0ps1/Sabeltann/discussions) or file an issue. We're happy to help orient new contributors.
