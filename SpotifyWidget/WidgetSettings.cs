using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace SpotifyWidget;

public sealed class WidgetSettings
{
    public string Placement { get; set; } = "AboveTaskbar";
    public double? Left { get; set; }
    public double? Top { get; set; }
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpotifyWidget");
    private static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static WidgetSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<WidgetSettings>(File.ReadAllText(FilePath)) ?? new();
            if (settings.Placement is not ("AboveTaskbar" or "Taskbar" or "Free")) settings.Placement = "AboveTaskbar";
            return settings;
        }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(this));
            File.Move(FilePath + ".tmp", FilePath, true);
        }
        catch (Exception ex) { App.Log(ex); }
    }

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool StartsWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("SpotifyWidget") is string;
    }
    public static void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue("SpotifyWidget", $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue("SpotifyWidget", false);
    }
}
