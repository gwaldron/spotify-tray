Fixes the player jumping out of the taskbar when you click the album art or song area.

- Normal clicks and small pointer movements keep the player docked.
- Deliberate dragging still moves the player and saves its new position.
- Double-clicking still opens Spotify, and playback buttons keep their normal behavior.
- Added regression checks for clicks, pointer jitter, released/cancelled gestures, and drag thresholds.

Download **SpotifyWidget-Setup-1.0.1-x64.exe** for the installer, or **SpotifyWidget-Portable-1.0.1-x64.zip** for a portable copy. Both include the .NET runtime.

If an earlier click already undocked your player, choose **… → Left side of taskbar (overlay)** once to dock it again.

The installer runs without administrator privileges, adds a Start menu shortcut, and supports uninstall through Windows Settings. Quit any running copy of the widget before installing or upgrading. Startup remains off until enabled in the widget's options menu.

These builds are unsigned. SHA-256 checksums are included in **SHA256SUMS.txt**. Requires Windows 10 version 2004 or later, or Windows 11, and the Spotify desktop app. The widget is independent of Spotify and is not affiliated with or endorsed by Spotify.
