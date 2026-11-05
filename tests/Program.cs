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
var gesture = new DragGesture();
Check(!gesture.TryStart(120, 20, true, 4, 4), "Moving over the widget without pressing it cannot undock it");
gesture.Begin(100, 20);
Check(!gesture.TryStart(100, 20, true, 4, 4), "Stationary click doesn't enter the native move loop");
Check(!gesture.TryStart(103, 23, true, 4, 4), "Small pointer jitter keeps the widget docked");
Check(!gesture.TryStart(100, 20, false, 4, 4), "Releasing a click doesn't start a drag");
Check(!gesture.TryStart(120, 20, true, 4, 4), "A released gesture cannot start later");
gesture.Begin(100, 20);
Check(gesture.TryStart(104, 20, true, 4, 4), "Deliberate horizontal drag starts at the Windows threshold");
Check(!gesture.TryStart(130, 20, true, 4, 4), "A drag starts only one native move loop");
gesture.Begin(100, 20);
Check(gesture.TryStart(100, 16, true, 4, 4), "Deliberate upward drag can move the widget off the taskbar");
gesture.Begin(100, 20);
gesture.Cancel();
Check(!gesture.TryStart(110, 20, true, 4, 4), "Lost capture or double-click cancels pending dragging");
gesture.Begin(100, 20);
Check(!gesture.TryStart(105, 20, true, 8, 8), "Larger system drag thresholds are respected");
Check(gesture.TryStart(108, 20, true, 8, 8), "Deliberate drag works with larger thresholds");
Console.WriteLine($"{checks} checks passed.");
