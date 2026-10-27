namespace SpotifyWidget;

public static class SpotifyIdentity
{
    // Match the desktop executable and the Microsoft Store package, never arbitrary browser sessions.
    public static bool IsSpotify(string id) =>
        id.Equals("Spotify.exe", StringComparison.OrdinalIgnoreCase) ||
        id.Equals("Spotify", StringComparison.OrdinalIgnoreCase) ||
        id.EndsWith(@"\Spotify.exe", StringComparison.OrdinalIgnoreCase) ||
        id.StartsWith("SpotifyAB.SpotifyMusic_", StringComparison.OrdinalIgnoreCase);
}
