using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpotifyWidget;

public partial class MainWindow : Window
{
    private readonly SpotifyMedia _media = new();
    private readonly WidgetSettings _settings = WidgetSettings.Load();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly SemaphoreSlim _mediaLock = new(1, 1);
    private bool _closed, _dragging, _positioning, _commandPending;
    private MediaState? _state;
    private nint _handle;
    private int _ticks;
    private DateTime _lastErrorLog;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _handle = new WindowInteropHelper(this).Handle;
            if (!Environment.GetCommandLineArgs().Contains("--inspect")) NativeWindow.MakeToolWindow(_handle);
            HwndSource.FromHwnd(_handle)?.AddHook(WindowMessage);
        };
        if (Environment.GetCommandLineArgs().Contains("--inspect")) ShowInTaskbar = true;
        Closed += (_, _) => { _closed = true; _timer.Stop(); };
        _timer.Tick += async (_, _) =>
        {
            if (++_ticks % 3 == 0 && !_dragging) ApplyPosition();
            await RefreshAsync();
        };
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_settings.Placement == "Free" && _settings.Left is double x && _settings.Top is double y && double.IsFinite(x) && double.IsFinite(y))
        {
            // Saved positions are physical pixels, independent of DPI on other monitors.
            NativeWindow.Place(_handle, (int)x, (int)y, (int)(Width * Scale), (int)(Height * Scale));
        }
        ApplyPosition();
        _timer.Start();
        await RefreshAsync();
    }

    private double Scale => Math.Max(96, NativeWindow.GetDpiForWindow(_handle)) / 96.0;
    private nint WindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message is 0x007E or 0x001A or 0x02E0 && !_positioning) // display, work area or DPI changed
            Dispatcher.BeginInvoke(() => { if (!_closed && !_dragging) ApplyPosition(); });
        return 0;
    }

    private void ApplyPosition()
    {
        if (_handle == 0 || _closed || _positioning) return;
        _positioning = true;
        try
        {
            var area = NativeWindow.WorkArea(_handle, _settings.Placement != "Free");
            double scale = Scale;
            var taskbar = _settings.Placement == "Taskbar" ? NativeWindow.HorizontalTaskbar() : null;
            if (taskbar is { } smallBar && smallBar.Height / scale < 44) taskbar = null;
            double height = taskbar is { } bar ? Math.Clamp(bar.Height / scale - 4, 40, 52) : 68;
            Height = height;
            ArtFrame.Width = ArtFrame.Height = height < 60 ? 32 : 42;
            Card.CornerRadius = new CornerRadius(height < 60 ? 7 : 12);
            ProgressTrack.Visibility = height < 60 ? Visibility.Collapsed : Visibility.Visible;
            double widthPx = Width * scale, heightPx = height * scale;
            (double X, double Y) point;
            if (taskbar is { } bounds)
                point = Placement.Clamp(bounds.Left + 8 * scale, bounds.Top + (bounds.Height - heightPx) / 2, widthPx, heightPx, bounds);
            else if (_settings.Placement == "Free")
                point = Placement.Clamp(_settings.Left ?? area.Left, _settings.Top ?? area.Top, widthPx, heightPx, area);
            else point = Placement.BottomLeft(widthPx, heightPx, area, 12 * scale);
            NativeWindow.Place(_handle, (int)Math.Round(point.X), (int)Math.Round(point.Y), (int)Math.Round(widthPx), (int)Math.Round(heightPx));
        }
        finally { _positioning = false; }
    }

    private async Task RefreshAsync()
    {
        if (_closed || !await _mediaLock.WaitAsync(0)) return;
        try
        {
            var state = await _media.ReadAsync();
            if (!_closed) Render(state);
        }
        catch (Exception ex)
        {
            if (DateTime.UtcNow - _lastErrorLog > TimeSpan.FromMinutes(1)) { App.Log(ex); _lastErrorLog = DateTime.UtcNow; }
            _media.Reconnect();
            if (!_closed) Render(new("Reconnecting to Spotify…", "Check that Spotify is open", false, false, false, false, false, 0, null));
        }
        finally { _mediaLock.Release(); }
    }

    private void Render(MediaState state)
    {
        _state = state;
        SongTitle.Text = state.Title; ArtistName.Text = state.Artist;
        SongTitle.ToolTip = state.Title; ArtistName.ToolTip = state.Artist;
        AlbumArt.Source = state.Artwork;
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(state.Playing ? "#1ED760" : "#7A857E"));
        StatusDot.ToolTip = state.Connected ? (state.Playing ? "Playing on Spotify" : "Paused") : "Waiting for Spotify";
        PlayGlyph.Data = Geometry.Parse(state.Playing ? "M 1,0 L 5,0 5,14 1,14 Z M 9,0 L 13,0 13,14 9,14 Z" : "M 2,0 L 12,7 2,14 Z");
        string label = state.Playing ? "Pause" : "Play";
        PlayButton.ToolTip = label; AutomationProperties.SetName(PlayButton, label);
        PlayButton.IsEnabled = state.CanPlayPause && !_commandPending;
        PreviousButton.IsEnabled = state.CanPrevious && !_commandPending;
        NextButton.IsEnabled = state.CanNext && !_commandPending;
        ProgressFill.Width = Math.Max(0, ProgressTrack.ActualWidth * state.Progress);
    }

    private async Task SendAsync(string command)
    {
        if (_commandPending || _closed) return;
        _commandPending = true;
        if (_state is not null) Render(_state);
        await _mediaLock.WaitAsync();
        try
        {
            if (!await _media.ControlAsync(command)) ShowNotice("Spotify couldn't perform that action. Try again in a moment.");
        }
        catch (Exception ex) { App.Log(ex); ShowNotice("Spotify is reconnecting. Try again in a moment."); _media.Reconnect(); }
        finally { _commandPending = false; _mediaLock.Release(); }
        await RefreshAsync();
    }

    private void ShowNotice(string message)
    {
        if (_closed) return;
        var tip = new ToolTip { Content = message, PlacementTarget = Card, Placement = System.Windows.Controls.Primitives.PlacementMode.Top, IsOpen = true, StaysOpen = true };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) => { tip.IsOpen = false; timer.Stop(); }; timer.Start();
    }

    private async void PlayPause(object sender, RoutedEventArgs e) => await SendAsync(_state?.Playing == true ? "pause" : "play");
    private async void Previous(object sender, RoutedEventArgs e) => await SendAsync("previous");
    private async void Next(object sender, RoutedEventArgs e) => await SendAsync("next");
    private void ShowOptions(object sender, RoutedEventArgs e) { OptionsMenu.PlacementTarget = Card; OptionsMenu.Placement = PlacementMode.Top; OptionsMenu.IsOpen = true; }
    private void OptionsOpened(object sender, RoutedEventArgs e)
    {
        AboveMenu.IsChecked = _settings.Placement == "AboveTaskbar";
        OverlayMenu.IsChecked = _settings.Placement == "Taskbar";
        try { StartupMenu.IsChecked = WidgetSettings.StartsWithWindows(); }
        catch (Exception ex) { App.Log(ex); }
    }
    private void AboveTaskbar(object sender, RoutedEventArgs e) => SetPlacement("AboveTaskbar");
    private void OnTaskbar(object sender, RoutedEventArgs e) => SetPlacement("Taskbar");
    private void SetPlacement(string placement) { _settings.Placement = placement; _settings.Save(); ApplyPosition(); }
    private void ToggleStartup(object sender, RoutedEventArgs e)
    {
        try { WidgetSettings.SetStartWithWindows(StartupMenu.IsChecked); }
        catch (Exception ex) { App.Log(ex); ShowNotice("Couldn't update startup. Try again or launch the widget manually."); }
    }
    private async void Reconnect(object sender, RoutedEventArgs e)
    {
        await _mediaLock.WaitAsync();
        try { _media.Reconnect(); } finally { _mediaLock.Release(); }
        await RefreshAsync();
    }
    private void OpenSpotify(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true }); }
        catch (Exception ex) { App.Log(ex); ShowNotice("Open the Spotify desktop app to connect."); }
    }
    private void Quit(object sender, RoutedEventArgs e) => Close();

    private void DragWidget(object sender, MouseButtonEventArgs e)
    {
        // Let buttons handle their own clicks; only the album and track area drag the window.
        for (DependencyObject? item = e.OriginalSource as DependencyObject; item is not null; item = VisualTreeHelper.GetParent(item))
            if (item is ButtonBase) return;
        if (e.ClickCount == 2) { OpenSpotify(sender, e); return; }
        _dragging = true;
        try
        {
            DragMove();
            var origin = PointToScreen(new Point(0, 0));
            _settings.Placement = "Free"; _settings.Left = origin.X; _settings.Top = origin.Y; _settings.Save();
        }
        catch (InvalidOperationException) { /* Mouse was released before the drag began. */ }
        finally { _dragging = false; }
        ApplyPosition();
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { OptionsMenu.IsOpen = false; e.Handled = true; }
        if (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift) { ShowOptions(sender, e); e.Handled = true; }
    }
}
