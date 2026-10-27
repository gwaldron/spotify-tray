# Spotify Widget

A small, always-visible Windows player for the Spotify desktop app. Shows the current song, artist, album art, playback status, and track progress, with previous, play/pause, and next buttons. No tray-icon click, browser, Spotify API key, or separate login is required.

## Run

Download the installer or portable ZIP from [GitHub Releases](https://github.com/gwaldron/spotify-tray/releases/latest).

**Installer:** Run `SpotifyWidget-Setup-1.0.0-x64.exe`. It installs for your user under `%LOCALAPPDATA%\Programs\SpotifyWidget`, adds a Start menu shortcut, and offers an optional desktop shortcut. No administrator access is required. Quit a running widget before installation or upgrades. Uninstall from Windows Settings → Apps; preferences are retained, and startup is removed only if it points to the uninstalled copy. Releases are currently unsigned.

**Portable:** Extract the ZIP and open `SpotifyWidget.exe`.

Open `dist\SpotifyWidget\SpotifyWidget.exe`. The default build includes its own .NET runtime and needs no installation or administrator access. Requires Windows 10 version 2004 or newer, or Windows 11, on x64, and the Spotify desktop app (Store or standalone). Play something in Spotify to create its Windows media session.

The player starts at the bottom left, just above the taskbar, and stays on top of normal windows. It remains visible when Spotify is paused or closed, and reconnects automatically. Windows secure screens and exclusive fullscreen apps can cover it.

- Drag the album art or song area to move it; the position is remembered.
- Click **…** or right-click for options, position reset, reconnect, and quit.
- Choose **Left side of taskbar (overlay)** for a shorter player over the taskbar's left end. This can cover Widgets, Start, or pinned apps, depending on your taskbar layout. Use the default above-taskbar mode if they overlap. Vertical taskbars fall back to above-taskbar placement.
- Choose **Start with Windows** to enable optional startup for the current user. Keep the executable in that location; toggle the setting again after moving it. Startup is off by default.
- Double-click the song area, or choose **Open Spotify**, to open Spotify.
- Tab between controls and press Enter/Space to activate. Shift+F10 opens options; Escape dismisses them.

Windows 11 does not provide the old custom taskbar toolbar/DeskBand integration, so this is a borderless topmost desktop widget, with an optional overlay, rather than an Explorer modification. See [Microsoft's feature deprecations](https://www.microsoft.com/en-us/windows/windows-11-specifications).

## Build and check

Install the .NET 10 SDK, then run:

```powershell
.\build.ps1
```

The script runs checks for Spotify identification and screen positioning, then creates a self-contained single executable under `dist\SpotifyWidget`. Use `-FrameworkDependent` for a smaller build requiring the .NET 10 Desktop Runtime. A workspace-local SDK under `.tools\dotnet` is also supported. No SDK is needed to run the self-contained output.

For development:

```powershell
dotnet run --project SpotifyWidget
dotnet run --project tests/SpotifyWidget.Checks.csproj -c Release
```

## Installer and GitHub releases

Install [Inno Setup](https://jrsoftware.org/isinfo.php) 6.3 or newer and run `./build-installer.ps1` (optionally `-ISCCPath` with the compiler location). It builds into `artifacts/publish`, leaving any running `dist` copy alone, and produces the installer, portable ZIP, and `SHA256SUMS.txt` in `artifacts/release`.

The Windows installer workflow builds on pushes to `main`, pull requests, and manual runs. It runs the application checks plus installation, bundled-runtime launch, upgrade, uninstall, shortcut, settings-preservation, and startup-cleanup checks on a disposable GitHub-hosted runner. Successful runs attach the **SpotifyWidget-Windows-x64** workflow artifact for 90 days. Installer smoke tests intentionally refuse to run on a normal workstation.

For a release, update `Version` in `SpotifyWidget/SpotifyWidget.csproj` and `docs/release-notes.md`, then push a matching `vX.Y.Z` tag. The workflow publishes the tested downloads as GitHub Release assets only after all checks pass. Tags that differ from the project version fail validation. Downloaded files can be verified against `SHA256SUMS.txt` using `Get-FileHash -Algorithm SHA256`.

## Implementation and troubleshooting

Uses WPF and Windows `GlobalSystemMediaTransportControlsSessionManager`. It selects only Spotify's desktop/Store sessions and sends commands directly to that session, so buttons do not pause another browser or media app. Spotify must publish its media session; unavailable transport commands are disabled. Album art is optional and supplied by Spotify. This is local desktop-session control, not Spotify Connect device management.

Metadata refreshes once per second. Album art is cached between track changes. Timeouts, missing artwork, Spotify restarts, monitor changes, and unavailable sessions are handled without hiding the player. Settings are in `%LOCALAPPDATA%\SpotifyWidget\settings.json`, and errors in `errors.log`. Optional startup uses the current user's Windows Run registry key.

If a track doesn't appear, play a song in the Spotify desktop app and choose **Reconnect to Spotify**. Run `SpotifyWidget.exe --diagnose` to write current session details to `%LOCALAPPDATA%\SpotifyWidget\diagnostics.json` (includes song/artist metadata; review before sharing). `--inspect` shows the same widget with a taskbar entry for accessibility/UI inspection. Only one normal widget instance can run per Windows session.

Manual checks: song/artist/artwork while playing, play/pause and next/previous, long-title truncation, missing artwork, Spotify close/reopen, drag and restart, taskbar overlay and reset, changed display scaling, monitor disconnect, and startup toggle. No network calls are made by the widget itself.
