using System.Collections.Generic;
using Newtonsoft.Json;
using Windows.Storage;

public class WatchLaterManager
{
    private const string WatchLaterKey = "WatchLaterVideos";

    public void AddWatchLaterVideo(string videoId)
    {
        var watchLaterVideos = GetWatchLaterVideos();

        if (!watchLaterVideos.Contains(videoId))
        {
            watchLaterVideos.Add(videoId);
            SaveWatchLaterVideos(watchLaterVideos);
        }
    }

    public bool IsVideoInWatchLater(string videoId)
    {
        var watchLaterVideos = GetWatchLaterVideos();
        return watchLaterVideos.Contains(videoId);
    }

    public List<string> GetWatchLaterVideos()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values.ContainsKey(WatchLaterKey))
        {
            return JsonConvert.DeserializeObject<List<string>>(localSettings.Values[WatchLaterKey].ToString());
        }

        return new List<string>();
    }

    public void RemoveWatchLaterVideo(string videoId)
    {
        var watchLaterVideos = GetWatchLaterVideos();

        if (watchLaterVideos.Remove(videoId))
        {
            SaveWatchLaterVideos(watchLaterVideos);
        }
    }

    private void SaveWatchLaterVideos(List<string> watchLaterVideos)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values[WatchLaterKey] = JsonConvert.SerializeObject(watchLaterVideos);
    }
}
