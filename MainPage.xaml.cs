using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ValleyTube
{
    public sealed partial class MainPage : Page
    {
        private List<VideoResult> _searchResults = new List<VideoResult>();
        private int _currentPage = 1;
        private const int _resultsPerPage = 20;

        public MainPage()
        {
            this.InitializeComponent(); 
            this.NavigationCacheMode = NavigationCacheMode.Required;
            LoadTrendingVideos();
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

                    if (page == 1)
                    {
                        _searchResults = searchResults;
                    }
                    else
                    {
                        _searchResults.AddRange(searchResults);
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
                    case "Trending":
                        LoadTrendingVideos();
                        break;
                    case "Gaming":
                        LoadGamingVideos();
                        break;
                    case "Music":
                        LoadMusicVideos();
                        break;
                    case "Movies":
                        LoadMoviesVideos();
                        break;
                    case "Search":
                        if (!string.IsNullOrEmpty(SearchBox.Text.Trim()))
                        {
                            Search(SearchBox.Text.Trim());
                        }
                        else
                        {
                            SearchListView.ItemsSource = null;
                        }
                        break;
                }
            }
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
                System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                Frame.Navigate(typeof(VideoPage), video.VideoId); 
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
    }

    public class Thumbnail
    {
        public string Quality { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
