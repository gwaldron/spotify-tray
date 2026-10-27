using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace SpotifyWidget;

public sealed record MediaState(string Title, string Artist, bool Connected, bool Playing,
    bool CanPlayPause, bool CanPrevious, bool CanNext, double Progress, BitmapSource? Artwork);

public sealed class SpotifyMedia
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private string? _artKey;
    private BitmapSource? _art;
    private DateTime _artAttempt;
    private static TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public async Task<MediaState> ReadAsync()
    {
        _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().WaitAsync(Timeout);
        var sessions = _manager.GetSessions().Where(s => SpotifyIdentity.IsSpotify(s.SourceAppUserModelId)).ToArray();
        var next = sessions.FirstOrDefault(s => s.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            ?? sessions.FirstOrDefault();
        if (next is null)
        {
            _session = null; _art = null; _artKey = null;
            return new("Spotify is quiet", "Open Spotify and play something", false, false, false, false, false, 0, null);
        }
        if (_session?.SourceAppUserModelId != next.SourceAppUserModelId) { _artKey = null; _art = null; }
        _session = next;
        var props = await next.TryGetMediaPropertiesAsync().AsTask().WaitAsync(Timeout);
        var playback = next.GetPlaybackInfo();
        bool playing = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        string artKey = $"{props.Title}\n{props.Artist}\n{props.AlbumTitle}";
        if (_artKey != artKey || (_art is null && DateTime.UtcNow - _artAttempt > TimeSpan.FromSeconds(10)))
        {
            _artKey = artKey; _art = null; _artAttempt = DateTime.UtcNow;
            if (props.Thumbnail is not null)
            {
                try { _art = await ReadArtworkAsync(props.Thumbnail).WaitAsync(Timeout); }
                catch { /* Missing or bad album art must never hide track information. */ }
            }
        }
        var timeline = next.GetTimelineProperties();
        double duration = (timeline.EndTime - timeline.StartTime).TotalSeconds;
        double position = (timeline.Position - timeline.StartTime).TotalSeconds;
        if (playing && timeline.LastUpdatedTime != default)
            position += Math.Max(0, (DateTimeOffset.UtcNow - timeline.LastUpdatedTime).TotalSeconds) * (playback.PlaybackRate ?? 1);
        var controls = playback.Controls;
        return new(string.IsNullOrWhiteSpace(props.Title) ? "Spotify" : props.Title,
            string.IsNullOrWhiteSpace(props.Artist) ? (playing ? "Playing on Spotify" : "Paused") : props.Artist,
            true, playing, playing ? controls.IsPauseEnabled : controls.IsPlayEnabled,
            controls.IsPreviousEnabled, controls.IsNextEnabled,
            duration > 0 ? Math.Clamp(position / duration, 0, 1) : 0, _art);
    }

    private static async Task<BitmapSource?> ReadArtworkAsync(IRandomAccessStreamReference reference)
    {
        using var stream = await reference.OpenReadAsync();
        if (stream.Size is 0 or > 8 * 1024 * 1024) return null;
        using var reader = new DataReader(stream);
        uint count = await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[count];
        reader.ReadBytes(bytes);
        using var memory = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 128;
        image.StreamSource = memory; image.EndInit(); image.Freeze();
        return image;
    }

    public async Task<bool> ControlAsync(string command)
    {
        var session = _session;
        if (session is null) return false;
        return command switch
        {
            "previous" => await session.TrySkipPreviousAsync().AsTask().WaitAsync(Timeout),
            "next" => await session.TrySkipNextAsync().AsTask().WaitAsync(Timeout),
            "pause" => await session.TryPauseAsync().AsTask().WaitAsync(Timeout),
            "play" => await session.TryPlayAsync().AsTask().WaitAsync(Timeout),
            _ => false
        };
    }

    public void Reconnect() { _manager = null; _session = null; _artKey = null; _art = null; }

    public static async Task WriteDiagnosticsAsync()
    {
        Directory.CreateDirectory(WidgetSettings.DirectoryPath);
        object result;
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().WaitAsync(Timeout);
            var sessions = new List<object>();
            foreach (var session in manager.GetSessions())
            {
                var media = await session.TryGetMediaPropertiesAsync().AsTask().WaitAsync(Timeout);
                sessions.Add(new { Source = session.SourceAppUserModelId, IsSpotify = SpotifyIdentity.IsSpotify(session.SourceAppUserModelId), media.Title, media.Artist,
                    Status = session.GetPlaybackInfo().PlaybackStatus.ToString(), HasArtwork = media.Thumbnail is not null });
            }
            result = new { Sessions = sessions };
        }
        catch (Exception ex) { result = new { Error = ex.ToString() }; }
        await File.WriteAllTextAsync(Path.Combine(WidgetSettings.DirectoryPath, "diagnostics.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}
