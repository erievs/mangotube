using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ValleyTube
{
    public sealed partial class MainPage : Page
    {
        private List<VideoResult> _searchResults = new List<VideoResult>();
        private int _currentPage = 1;
        private const int _resultsPerPage = 20;
        private static List<VideoResult> _videoHistory = new List<VideoResult>();

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            LoadTrendingVideos();
            LoadHistory();
            SubscriptionManager.LoadSubscriptions();
        }

        private async void LoadTrendingVideos()
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/trending";
            await FetchData(apiUrl, TrendingListView);
        }
        private async void LoadPopularVideos()
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/trending";
            await FetchData(apiUrl, PopularListView);
        }

        private async void LoadGamingVideos()
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/trending?type=gaming";
            await FetchData(apiUrl, GamingListView);
        }

        private async void LoadSubscribedChannelsVideos()
        {
            try
            {
                var videos = await GetSubscribedChannelsVideosAsync();

                SubscriptionsListView.ItemsSource = videos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load subscribed channels' videos: " + ex.Message);
            }
        }

        private async void LoadMusicVideos()
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/trending?type=music";
            await FetchData(apiUrl, MusicListView);
        }

        private async void LoadMoviesVideos()
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/trending?type=movies";
            await FetchData(apiUrl, MoviesListView);
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

        public static async Task<List<VideoResult>> FetchLatestVideosAsync(string authorId)
        {
            string apiUrl = Settings.InvidiousInstance + $"/api/v1/channels/{authorId}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var channel = JsonConvert.DeserializeObject<Channel>(response);

                    var limitedVideos = new List<VideoResult>();

                    for (int i = 0; i < channel.LatestVideos.Count && i < 5; i++)
                    {
                        limitedVideos.Add(channel.LatestVideos[i]);
                    }

                    return limitedVideos;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching channel data: " + ex.Message);
                    return new List<VideoResult>();
                }
            }
        }

        public static async Task<List<VideoResult>> GetSubscribedChannelsVideosAsync()
        {
            var videos = new List<VideoResult>();

            foreach (var author in SubscriptionManager.SubscribedAuthors)
            {
                var latestVideos = await FetchLatestVideosAsync(author);
                videos.AddRange(latestVideos);
            }

            videos.Sort(CompareVideosByPublished);

            return videos;
        }

        private static int CompareVideosByPublished(VideoResult v1, VideoResult v2)
        {
            return v2.Published.CompareTo(v1.Published);
        }

        private async void Search(string query, int page = 1)
        {
            string apiUrl = string.Format(Settings.InvidiousInstance + "/api/v1/search?q={0}&page={1}&sort=relevance",
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

        private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Pivot pivot = sender as Pivot;
            if (pivot == null)
            {
                System.Diagnostics.Debug.WriteLine("Sender is not a Pivot.");
                return;
            }

            PivotItem selectedItem = pivot.SelectedItem as PivotItem;
            if (selectedItem == null)
            {
                System.Diagnostics.Debug.WriteLine("Selected item is not a PivotItem.");
                return;
            }

            switch (selectedItem.Header.ToString())
            {
                case "trending":
                    LoadTrendingVideos();
                    break;
                case "popular":
                    LoadPopularVideos();
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
                case "history":
                    LoadHistory();
                    break;
                case "subscriptions":
                    LoadSubscribedChannelsVideos();
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unhandled PivotItem header: " + selectedItem.Header);
                    break;
            }
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

        private async void ShowShit(string message)
        {

            StatusMessageTextBlock.Text = message;
            StatusMessageTextBlock.Visibility = Visibility.Visible;

            await Task.Delay(3000);

            StatusMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        // Buttons and Similar Ui Elements

        private async void ClearSubcriptions_Click(object sender, RoutedEventArgs e)
        {
            SubscriptionManager.ClearSubscriptions();
            SubscriptionManager.SaveSubscriptions();

        }

        private void AutoplayToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.IsAutoplayEnabled = AutoplayToggleSwitch.IsOn;
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

        private void ImportSubscriptionsButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.FileTypeFilter.Add(".csv");

            filePicker.PickSingleFileAndContinue();
        }
    
        private void QualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBox comboBox = sender as ComboBox;

            System.Diagnostics.Debug.WriteLine("Sender type: " + (comboBox != null ? comboBox.GetType().ToString() : "null"));

            if (comboBox != null)
            {

                ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;

                if (selectedItem != null)
                {

                    System.Diagnostics.Debug.WriteLine("Selected item Tag: " + selectedItem.Tag.ToString());

                    string qualityTag = selectedItem.Tag.ToString();

                    System.Diagnostics.Debug.WriteLine("Quality Tag: " + qualityTag);

                    Settings.SelectedQuality = qualityTag;
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine("Selected item is not a ComboBoxItem.");
                }
            }
            else
            {

                System.Diagnostics.Debug.WriteLine("ComboBox is null.");
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
        public string VideoId { get; set; }
        public string author { get; set; }
        public string published { get; set; }
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
        public string authorId { get; set; }
        public string Description { get; set; }
        public string ViewCountText { get; set; }
        public string PublishedText { get; set; }
        public Thumbnail Thumbnail { get; set; }
        public List<VideoThumbnail> VideoThumbnails { get; set; }
        public bool IsFavorite { get; set; }
        public long Published { get; set; }
    }

    public class Channel
    {
        public string Author { get; set; }
        public string AuthorId { get; set; }
        public List<VideoResult> LatestVideos { get; set; }
    }

    public class VideoObject
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public Thumbnail Thumbnail { get; set; }
    }

    public class Thumbnail
    {
        public string Quality { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}