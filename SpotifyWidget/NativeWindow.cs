using System.Runtime.InteropServices;

namespace SpotifyWidget;

internal static class NativeWindow
{
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; public readonly ScreenRect Area => new(Left, Top, Right, Bottom); }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindow(string className, string? title);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(nint window, int index, int value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);

    internal static void MakeToolWindow(nint window) => SetWindowLong(window, -20, GetWindowLong(window, -20) | 0x80);
    internal static void Place(nint window, int x, int y, int width, int height) =>
        SetWindowPos(window, new nint(-1), x, y, width, height, 0x10); // HWND_TOPMOST, SWP_NOACTIVATE
    internal static ScreenRect WorkArea(nint window, bool primary)
    {
        nint monitor = primary ? MonitorFromPoint(new Point(), 1) : MonitorFromWindow(window, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) throw new InvalidOperationException("Unable to read monitor dimensions.");
        return info.Work.Area;
    }
    internal static ScreenRect? HorizontalTaskbar()
    {
        nint taskbar = FindWindow("Shell_TrayWnd", null);
        return taskbar != 0 && GetWindowRect(taskbar, out var rect) && rect.Right - rect.Left > rect.Bottom - rect.Top
            && rect.Bottom - rect.Top >= 30 ? rect.Area : null;
    }
}
