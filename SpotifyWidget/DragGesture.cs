namespace SpotifyWidget;

/// <summary>Distinguishes a click from an intentional drag before entering Windows' move loop.</summary>
public sealed class DragGesture
{
    private (double X, double Y)? _start;

    public void Begin(double x, double y) => _start = (x, y);
    public void Cancel() => _start = null;

    public bool CompleteClick(double x, double y, double horizontalThreshold, double verticalThreshold)
    {
        var start = _start;
        Cancel();
        return start is { } point
            && Math.Abs(x - point.X) < horizontalThreshold
            && Math.Abs(y - point.Y) < verticalThreshold;
    }

    public bool TryStart(double x, double y, bool leftButtonPressed, double horizontalThreshold, double verticalThreshold)
    {
        if (!leftButtonPressed) { Cancel(); return false; }
        if (_start is not { } start) return false;
        if (Math.Abs(x - start.X) < horizontalThreshold && Math.Abs(y - start.Y) < verticalThreshold) return false;
        Cancel(); // Consume the gesture so it cannot start a second move loop.
        return true;
    }
}
