# Security Policy

## Supported versions

Only the latest release is actively supported with security fixes.

| Version | Supported |
|---------|-----------|
| Latest release | Yes |
| Older releases | No |

## Reporting a vulnerability

**Please do not report security vulnerabilities as public GitHub issues.**

Instead, use GitHub's private vulnerability reporting:

**[Report a vulnerability privately](https://github.com/adr0ps1/Sabeltann/security/advisories/new)**

This opens a private security advisory that is only visible to repository maintainers until a fix is released.

### What to include

- A clear description of the vulnerability
- Steps to reproduce or a proof-of-concept
- The version of Sabeltann affected
- Potential impact (what an attacker could do)

### Response time

This is an open-source project maintained in spare time. We aim to acknowledge reports within **7 days** and will communicate a remediation timeline as soon as we've assessed the issue. We appreciate your patience.

## Scope

### In scope

- The Sabeltann application itself (playback, UI, settings storage)
- LibVLC integration and how the app invokes it
- Xtream Codes API credential handling (how credentials are stored, transmitted, or logged)
- M3U playlist parsing and URL handling
- Local settings/cache storage in `%LocalAppData%\Sabeltann\`
- Update mechanism (Velopack integration)

### Out of scope

- Vulnerabilities in third-party services (Xtream Codes providers, IPTV services, VLC itself)
- Issues with the user's IPTV provider or playlist source
- Social engineering attacks
- Physical access attacks

## Disclosure policy

We follow responsible disclosure. Once a fix is available, we will publish a security advisory crediting the reporter (unless they prefer to remain anonymous).
