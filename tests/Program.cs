using SpotifyWidget;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
    Console.WriteLine($"PASS {name}");
}

foreach (var id in new[] { "Spotify.exe", "SPOTIFY.EXE", @"C:\Users\test\Spotify.exe", "SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify" })
    Check(SpotifyIdentity.IsSpotify(id), $"Recognizes local Spotify: {id}");
foreach (var id in new[] { "chrome.exe", "msedge.exe", "YouTube", "FakeSpotify.exe", "SpotifyHelper.exe", "" })
    Check(!SpotifyIdentity.IsSpotify(id), $"Doesn't control another player: {id}");

var primary = new ScreenRect(0, 0, 1920, 1032);
Check(Placement.BottomLeft(400, 68, primary, 12) == (12, 952), "Default placement clears the taskbar");
var secondary = new ScreenRect(-2560, -240, 0, 1160);
var negative = Placement.BottomLeft(600, 102, secondary, 18);
Check(negative == (-2542, 1040), "Negative monitor coordinates and 150% scaling");
Check(Placement.Clamp(5000, -5000, 400, 68, primary) == (1520, 0), "Offscreen saved position returns to a visible monitor");
Check(Placement.Clamp(double.NaN, double.PositiveInfinity, 400, 68, primary) == (0, 0), "Invalid positions recover safely");
Check(Placement.Clamp(100, 100, 400, 68, new(0, 0, 200, 50)) == (0, 0), "Small display doesn't throw");
foreach (double scale in new[] { 1.0, 1.25, 1.5, 2.0 })
{
    var area = new ScreenRect(-1920, 0, 1920, 2160 - 48 * scale);
    var p = Placement.BottomLeft(400 * scale, 68 * scale, area, 12 * scale);
    Check(p.X >= area.Left && p.Y >= area.Top && p.X + 400 * scale <= area.Right && p.Y + 68 * scale <= area.Bottom,
        $"Widget stays within the work area at {scale:P0}");
}
Console.WriteLine($"{checks} checks passed.");
