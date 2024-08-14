using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Newtonsoft.Json;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Media;

namespace ValleyTube
{
    public sealed partial class MainPage : Page
    {
        private List<VideoResult> _searchResults = new List<VideoResult>();
        private int _currentPage = 1;
        private const int _resultsPerPage = 20;
        private Dictionary<string, bool> favoriteVideos = new Dictionary<string, bool>();
        private static List<VideoResult> _videoHistory = new List<VideoResult>();

        public MainPage()
        {
            this.InitializeComponent(); 
            this.NavigationCacheMode = NavigationCacheMode.Required;
            LoadTrendingVideos();
            LoadFavoritesFromStorage();
        }

        private async void LoadTrendingVideos()
        {
            string apiUrl = "https://inv.nadeko.net/api/v1/trending";
            await FetchData(apiUrl, TrendsListView);
        }

        private async void LoadGamingVideos()
        {
            string apiUrl = "https://inv.nadeko.net/api/v1/trending?type=gaming";
            await FetchData(apiUrl, GamingListView);
        }

        private async void LoadMusicVideos()
        {
            string apiUrl = "https://inv.nadeko.net/api/v1/trending?type=music";
            await FetchData(apiUrl, MusicListView);
        }

        private async void LoadMoviesVideos()
        {
            string apiUrl = "https://inv.nadeko.net/api/v1/trending?type=movies";
            await FetchData(apiUrl, MoviesListView);
        }

        private async void LoadFavorites()
        {
            var videoDetails = await FetchFavoriteVideosAsync(favoriteVideos);

            FavoritesListView.ItemsSource = videoDetails;

            var videoIdList = string.Join(",", favoriteVideos.Keys);
            System.Diagnostics.Debug.WriteLine("Video IDs: " + videoIdList);
        }

        private void LoadHistory()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("VideoHistory"))
            {
                string jsonHistory = localSettings.Values["VideoHistory"] as string;

                if (!string.IsNullOrEmpty(jsonHistory))
                {
                    _videoHistory = JsonConvert.DeserializeObject<List<VideoResult>>(jsonHistory);
                    HistoryListView.ItemsSource = _videoHistory;
                }
            }
        }

        private async Task FetchData(string apiUrl, ListView listView)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var videos = JsonConvert.DeserializeObject<List<TrendingVideo>>(response);

                    foreach (var video in videos)
                    {
                        System.Diagnostics.Debug.WriteLine("Video ID: " + video.VideoId);
                    }

                    listView.ItemsSource = videos;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching data: " + ex.Message);
                }
            }
        }

        private async void Search(string query, int page = 1)
        {
            string apiUrl = string.Format("https://inv.nadeko.net/api/v1/search?q={0}&page={1}&sort=relevance",
                                            Uri.EscapeDataString(query), page);

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var searchResults = JsonConvert.DeserializeObject<List<VideoResult>>(response);

                    var validResults = new List<VideoResult>();
                    foreach (var video in searchResults)
                    {
                        if (!string.IsNullOrEmpty(video.VideoId))
                        {
                            validResults.Add(video);
                        }
                    }

                    if (page == 1)
                    {
                        _searchResults = validResults;
                    }
                    else
                    {
                        _searchResults.AddRange(validResults);
                    }

                    SearchListView.ItemsSource = null;
                    SearchListView.ItemsSource = _searchResults;

                    _currentPage = page;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching search results: " + ex.Message);
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            var query = searchBox.Text;

            if (!string.IsNullOrWhiteSpace(query))
            {
                _currentPage = 1; 
                Search(query);
            }
            else
            {
                _searchResults.Clear();
                SearchListView.ItemsSource = null;
            }
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                _currentPage++;
                Search(query, _currentPage);
            }
        }



        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = sender as Pivot;
            PivotItem selectedItem = pivot.SelectedItem as PivotItem;

            if (selectedItem != null)
            {
                switch (selectedItem.Header.ToString())
                {
                    case "trending":
                        LoadTrendingVideos();
                        break;
                    case "gaming":
                        LoadGamingVideos();
                        break;
                    case "music":
                        LoadMusicVideos();
                        break;
                    case "movies":
                        LoadMoviesVideos();
                        break;
                    case "search":
                        if (!string.IsNullOrEmpty(SearchBox.Text.Trim()))
                        {
                            Search(SearchBox.Text.Trim());
                        }
                        else
                        {
                            SearchListView.ItemsSource = null;
                        }
                        break;
                    case "favorites":
                        LoadFavorites();
                        break;
                    case "history":
                        LoadHistory();
                        break;
                }
            }
        }

        private async void ShowShit(string message)
        {

            StatusMessageTextBlock.Text = message;
            StatusMessageTextBlock.Visibility = Visibility.Visible;

            await Task.Delay(3000);

            StatusMessageTextBlock.Visibility = Visibility.Collapsed;
        }



        private void TrendsListView_ItemClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            TrendingVideo video = button != null ? button.DataContext as TrendingVideo : null;

            if (video != null)
            {
                Frame.Navigate(typeof(VideoPage), video.VideoId);
                SaveHistoryTrend(video);
            }

        }

        private void ImageButton_Click2(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            VideoResult video = button != null ? button.DataContext as VideoResult : null;

            if (video != null)
            {
                Frame.Navigate(typeof(VideoPage), video.VideoId);
                SaveHistory(video);
            }
        }


        private async Task<List<VideoResult>> FetchFavoriteVideosAsync(Dictionary<string, bool> videoIdsDict)
        {
            var videoResults = new List<VideoResult>();
            var videoIds = new List<string>(videoIdsDict.Keys);

            videoIds.RemoveAll(id => string.IsNullOrEmpty(id));

            foreach (var videoId in videoIds)
            {
                var requestUrl = "https://inv.nadeko.net/api/v1/videos/" + videoId;
                System.Diagnostics.Debug.WriteLine("Request URL: " + requestUrl);

                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        var response = await client.GetStringAsync(requestUrl);
                        System.Diagnostics.Debug.WriteLine("Response: " + response);

                        var videoResult = JsonConvert.DeserializeObject<VideoResult>(response);
                        if (videoResult != null)
                        {
                            videoResults.Add(videoResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error fetching video " + videoId + ": " + ex.Message);
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Video Results Count: " + videoResults.Count);
            return videoResults;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                VideoResult video = button.DataContext as VideoResult;

                if (video != null)
                {
                    System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                    Frame.Navigate(typeof(VideoPage), video.VideoId);
                    SaveHistory(video);
                }
            }
        }

        private void Button_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            // to make it happy
        }


        private void ClearHistory()
        {
            _videoHistory.Clear();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["VideoHistory"] = null;
        }

        public static void SaveHistory(VideoResult video)
        {
            AddVideoToHistory(video);
        }

        private void SaveHistoryTrend(TrendingVideo video)
        {
            var videoResult = new VideoResult
            {
                VideoId = video.VideoId,
                Title = video.Title,
                Author = video.Author,
                Thumbnail = new ValleyTube.Thumbnail
                {
                    Url = video.ThumbnailUrl
                }
            };

            AddVideoToHistory(videoResult);
        }

        private static void AddVideoToHistory(VideoResult video)
        {

            if (_videoHistory.Count >= Settings.MaxHistorySize)
            {
                _videoHistory.RemoveAt(0);
            }

            _videoHistory.Add(video);

            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string jsonHistory = JsonConvert.SerializeObject(_videoHistory);
                localSettings.Values["VideoHistory"] = jsonHistory;
            }
            catch (Exception ex)
            {

                Debug.WriteLine("Error saving video history: " + ex.Message);
            }
        }

        private void FavoriteButton_Click_1(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                TrendingVideo video = button.DataContext as TrendingVideo;
                if (video != null)
                {
                    if (favoriteVideos.ContainsKey(video.VideoId))
                    {

                        favoriteVideos.Remove(video.VideoId);
                    }
                    else
                    {

                        favoriteVideos.Add(video.VideoId, true);
                    }

                    UpdateFavoriteButtonText(button, video.VideoId);

                    LoadFavorites();
                }
            }
        }

        private void FavoriteButton_Click_2(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                VideoResult video = button.DataContext as VideoResult;

                if (video != null && !string.IsNullOrEmpty(video.VideoId))
                {
                    try
                    {
                        if (favoriteVideos == null)
                        {
                            favoriteVideos = new Dictionary<string, bool>();
                        }

                        if (favoriteVideos.ContainsKey(video.VideoId))
                        {
                            favoriteVideos.Remove(video.VideoId);
                        }
                        else
                        {
                            favoriteVideos.Add(video.VideoId, true);
                        }

                        UpdateFavoriteButtonText(button, video.VideoId);

                        LoadFavorites();
                    }
                    catch (Exception ex)
                    {

                        System.Diagnostics.Debug.WriteLine("An error occurred: " + ex.Message);
                    }
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine("Video or VideoId is null.");
                }
            }
            else
            {

                System.Diagnostics.Debug.WriteLine("Sender is not a Button.");
            }
        }

        private void DashVideoToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.isDash = DashVideoToggleSwitch.IsOn;
        }

        private void UpdateFavoriteButtonText(Button button, string videoId)
        {
            if (favoriteVideos.ContainsKey(videoId))
            {
                button.Content = "UNFAVORITE";
                SaveFavorites();
            }
            else
            {
                button.Content = "FAVORITE";
                SaveFavorites();
            }
        }

        private void SaveFavorites()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            string jsonFavorites = JsonConvert.SerializeObject(favoriteVideos);

            System.Diagnostics.Debug.WriteLine("Saving favorites: " + jsonFavorites);

            localSettings.Values["FavoriteVideos"] = jsonFavorites;

            System.Diagnostics.Debug.WriteLine("Favorites saved successfully.");
        }

        private void LoadFavoritesFromStorage()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("FavoriteVideos"))
            {

                string jsonFavorites = localSettings.Values["FavoriteVideos"] as string;

                if (!string.IsNullOrEmpty(jsonFavorites))
                {
                    favoriteVideos = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jsonFavorites);
                }
            }

        } 
    }


    public class SearchResult
    {
        public List<VideoResult> Results { get; set; }
    }


    public class VideoDetail
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string hlsUrl { get; set; }
        public List<VideoFormat> adaptiveFormats { get; set; }
        public List<VideoFormat> formatStreams { get; set; }
        public List<VideoResult> recommendedVideos { get; set; } 
    }

    public class VideoFormat
    {
        public string url { get; set; }
        public string itag { get; set; }
        public string type { get; set; }
        public string quality { get; set; }
        public string container { get; set; }
        public string encoding { get; set; }
        public string qualityLabel { get; set; }
        public string resolution { get; set; }
    }

    public class VideoThumbnail
    {
        public string Quality { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class VideoResult
    {
        public string VideoId { get; set; }
        public string Id { get; set; }  
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string ViewCountText { get; set; }
        public string PublishedText { get; set; }
        public Thumbnail Thumbnail { get; set; }
        public List<VideoThumbnail> VideoThumbnails { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class Thumbnail
    {
        public string Quality { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}