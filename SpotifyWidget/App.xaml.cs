using System.IO;
using System.Windows;

namespace SpotifyWidget;

public partial class App : Application
{
    private Mutex? _instance;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--diagnose"))
        {
            await SpotifyMedia.WriteDiagnosticsAsync();
            Shutdown();
            return;
        }
        _instance = new Mutex(true, @"Local\SpotifyWidget.Desktop", out bool created);
        if (!created) { Shutdown(); return; }
        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            // Unexpected UI faults should be visible instead of leaving a silently broken player.
            MessageBox.Show("The widget ran into a problem. Restart it to reconnect to Spotify.\n\n" + args.Exception.Message,
                "Spotify Widget", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    internal static void Log(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(WidgetSettings.DirectoryPath);
            File.AppendAllText(Path.Combine(WidgetSettings.DirectoryPath, "errors.log"), $"{DateTimeOffset.Now:O} {ex}\n");
        }
        catch { /* Logging must not interrupt playback. */ }
    }

    protected override void OnExit(ExitEventArgs e) { _instance?.Dispose(); base.OnExit(e); }
}
