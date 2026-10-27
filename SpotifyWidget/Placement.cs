namespace SpotifyWidget;

public readonly record struct ScreenRect(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
}

public static class Placement
{
    public static (double X, double Y) Clamp(double x, double y, double width, double height, ScreenRect area) =>
        (Math.Clamp(double.IsFinite(x) ? x : area.Left, area.Left, Math.Max(area.Left, area.Right - width)),
         Math.Clamp(double.IsFinite(y) ? y : area.Top, area.Top, Math.Max(area.Top, area.Bottom - height)));

    public static (double X, double Y) BottomLeft(double width, double height, ScreenRect area, double margin) =>
        Clamp(area.Left + margin, area.Bottom - height - margin, width, height, area);
}
