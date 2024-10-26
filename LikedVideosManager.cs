using System.Collections.Generic;
using Newtonsoft.Json;
using Windows.Storage;

public class LikedVideosManager
{
    private const string LikedVideosKey = "LikedVideos";

    public void AddLikedVideo(string videoId)
    {
        var likedVideos = GetLikedVideos();

        if (!likedVideos.Contains(videoId))
        {
            likedVideos.Add(videoId);
            SaveLikedVideos(likedVideos);
        }
    }

    public bool IsVideoLiked(string videoId)
    {
        var likedVideos = GetLikedVideos();
        return likedVideos.Contains(videoId);
    }

    public List<string> GetLikedVideos()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        if (localSettings.Values.ContainsKey(LikedVideosKey))
        {
            return JsonConvert.DeserializeObject<List<string>>(localSettings.Values[LikedVideosKey].ToString());
        }

        return new List<string>();
    }

    public void RemoveLikedVideo(string videoId)
    {
        var likedVideos = GetLikedVideos();

        if (likedVideos.Remove(videoId))
        {
            SaveLikedVideos(likedVideos);
        }
    }

    private void SaveLikedVideos(List<string> likedVideos)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values[LikedVideosKey] = JsonConvert.SerializeObject(likedVideos);
    }
}