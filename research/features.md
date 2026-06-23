# IPTV Player Feature Research

Features present in popular IPTV players (TiviMate, OTT Navigator, GSE Smart IPTV, Perfect Player,
Kodi PVR, Sparkle TV, IPTV Smarters, StreamVault, IPTVnator) that Sabeltann does not yet have.
Pre-existing Sabeltann features are excluded.

---

## EPG / TV Guide

- **XMLTV EPG loading** — Parse an XMLTV (`.xml`/`.xmltv`) URL or file to populate program schedule data against live channels matched by `tvg-id`.
- **Now/Next display on channel cards** — Show the currently-airing program title and a progress bar on each channel tile, sourced from EPG data.
- **EPG timeline grid view** — Full TV-guide–style grid (channels on Y-axis, time on X-axis) so users can browse what is on across all channels at a glance.
- **Catch-up / archive playback** — Play previously aired programs via provider time-shift URLs embedded in the M3U (`catchup-source`, `catchup-days` tags), if the provider supports it.
- **Timeshift (pause/rewind live TV)** — Buffer the live stream to a temp file so the user can pause, rewind, and resume a live channel as if it were a recording.
- **Program search across EPG** — Search the loaded EPG data by program title to find when and where something airs, across all channels.

---

## Playback

- **Audio track selection** — In-player UI to switch between multiple audio tracks or language dubs present in a stream (LibVLC already exposes this via `MediaPlayer.AudioTrackList`).
- **Subtitle track selection** — In-player UI to enable/disable and switch embedded subtitle tracks (DVB, SRT, etc.) within the stream.
- **External subtitle loading** — Let the user load a local `.srt`/`.ass` file and overlay it on the current video.
- **Playback speed control** — Speed selector (0.5×, 0.75×, 1×, 1.5×, 2×) for VOD content, exposed via `MediaPlayer.Rate`.
- **Hardware decoding mode toggle** — Settings option to choose between software, hardware (DXVA2/D3D11), or auto decoding in LibVLC, useful when a GPU cannot decode an exotic codec.
- **Auto-play next episode** — When a series episode finishes, automatically start the next episode after a short countdown overlay (e.g., 10-second cancel prompt).
- **Resume VOD from last position** — Persist the playback timestamp of each VOD to disk and offer to resume from that point on next play, not just restore which VOD was last opened.
- **Sleep timer** — Auto-stop playback after a user-selected duration (15 / 30 / 60 / 90 / 120 min) with a countdown warning before stopping.
- **Picture-in-Picture (PiP) mode** — Float a small always-on-top player window so the user can browse channels or the UI while watching.

---

## Recording / DVR

- **Live stream recording** — Capture the current live stream to a local file (LibVLC supports `sout` to file); let the user choose save path.
- **Scheduled recording** — Set a future start/end time tied to an EPG entry; a background timer starts and stops the recording automatically.

---

## Content Management

- **Multiple playlists / sources** — Store and switch between more than one M3U URL, file, or Xtream account without re-entering credentials each time.
- **Automatic playlist refresh** — Re-fetch the M3U source on a configurable schedule (e.g., every 24 hours) so new/removed channels appear without manual reload.
- **Channel hiding** — Mark individual channels as hidden so they are excluded from all views without being deleted from the underlying playlist.
- **Channel renaming** — Override a channel's display name with a user-supplied name, persisted in settings.
- **Manual channel ordering** — Let users drag-and-drop or explicitly reorder channels within a group, overriding the playlist order.
- **Custom groups / categories** — Allow users to create their own named groups and assign channels to them, in addition to the provider's groups.
- **Export/import settings and favorites** — Serialize settings, favorites, and hidden channels to a portable JSON file for backup or device migration.

---

## UI / UX

- **Recently watched** — A dedicated row or section showing the last N channels/VODs opened, for quick re-access without searching.
- **Continue watching row** — Netflix-style horizontal strip on the Movies/Series home showing in-progress VODs with a progress indicator.
- **Channel number jump** — Type a numeric channel number (from the M3U `tvg-chno` tag) to jump directly to that channel, the way a traditional TV remote works.
- **Mini-player / windowed overlay** — A compact always-on-top window mode (smaller than full PiP) for monitoring a stream while working.
- **Theme customization** — Allow switching between dark/light themes, or letting the user pick accent colors, beyond the current fixed dark palette.
- **Font / UI scale setting** — Setting to increase text size for accessibility or large-screen use.
- **Keyboard shortcut reference / customization** — An in-app shortcut list (e.g., `?` key) and optionally user-remappable bindings.
- **Windows media key support** — Handle `VK_MEDIA_PLAY_PAUSE`, `VK_MEDIA_STOP`, `VK_MEDIA_NEXT_TRACK` so hardware media keys and taskbar transport controls work.

---

## Parental Controls

- **PIN-protected channels or categories** — Lock specific channels or entire groups behind a 4-digit PIN, prompted before playback.
- **Adult content auto-flagging** — Detect channels whose group name matches adult keywords and require PIN or explicit opt-in to load them.

---

## Content Discovery

- **TMDB metadata integration** — Use The Movie Database (free API) as an alternative/supplement to OMDb for richer movie and series metadata (trailers, cast images, seasons/episodes list).
- **Watch history** — Track and display a full history of what was watched and when, clearable by the user.
- **Similar / related content suggestions** — On the movie detail page, suggest other movies or shows from the same genre or director that exist in the current playlist.

---

## Network / Performance

- **Backup stream URLs** — Support multiple stream URLs per channel (via `url-tvg` extras or a custom tag) and automatically try the next if the primary fails.
- **Configurable reconnect / retry policy** — Settings for how many times and how quickly to retry a failed stream before showing "Stream unavailable".
- **Stream bandwidth test** — Built-in speed test that measures the connection to a given server, helping users diagnose buffering before contacting their provider.
- **Proxy / VPN passthrough setting** — Allow specifying an HTTP proxy for stream requests, useful when a provider blocks direct IPs.
- **Adaptive stream buffer size** — Expose a slider or preset (Low / Normal / High) that maps to LibVLC's `network-caching` option to tune buffering for slow connections.
