﻿using Newtonsoft.Json;
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
using Windows.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Globalization;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls.Primitives;

namespace ValleyTube
{
    public sealed partial class MainPage : Page
    {

        private static List<VideoResult> _videoHistory = new List<VideoResult>();
        public Windows.System.Display.DisplayRequest displayRequest = null;

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = typeof(Settings);
            this.NavigationCacheMode = NavigationCacheMode.Required;
            LoadTrendingVideos();
            LoadHistory();
            Settings.LoadSettings();
            SubscriptionManager.LoadSubscriptions();

            InvidiousInstanceTextBox.Text = Settings.InvidiousInstance;
            InvidiousInstanceCommentsTextBox.Text = Settings.InvidiousInstanceComments;
            ReturnDislikesTextBox.Text = Settings.ReturnDislikeInstance;
            SponserblockTextBox.Text = Settings.SponserBlockInstance;
            TimeoffToggleSwitch.IsOn = Settings.ScreenTimeOut;
            AutoplayToggleSwitch.IsOn = Settings.IsAutoplayEnabled;
            SponserblockSwitch.IsOn = Settings.isSponserBlock;
            ShowSponserSkipSwitch.IsOn = Settings.showSponserSkipMessage;
            DoulbeTapToSkipkSwitchUggh.IsOn = Settings.doubleTapToSkip;
            SubbedVideosTextBox.Text = Settings.HowManySubbedVideosToFetch.ToString();
            RecommendedVideosTextBox.Text = Settings.RecommendVideoLimit.ToString();


            System.Diagnostics.Debug.WriteLine(Settings.ScreenTimeOut);

            InitializeDisplayRequest();

            SetComboBoxSelection(QualityComboBox, Settings.SelectedQuality);

        }

        private void SetComboBoxSelection(ComboBox comboBox, string selectedQuality)
        {

            string qualityToSelect = string.IsNullOrEmpty(selectedQuality) ? "360p-direct" : selectedQuality;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if ((string)item.Tag == qualityToSelect)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void InitializeDisplayRequest()
        {
            if (Settings.ScreenTimeOut)
            {

                if (displayRequest != null)
                {
                    displayRequest.RequestRelease();
                    displayRequest = null;
                }
            }
            else
            {

                if (displayRequest == null)
                {
                    displayRequest = new Windows.System.Display.DisplayRequest();
                    displayRequest.RequestActive();
                }
            }
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

                    System.Diagnostics.Debug.WriteLine("Fetching channel data from URL: " + apiUrl);

                    var response = await client.GetStringAsync(apiUrl);
                    var channel = JsonConvert.DeserializeObject<Channel>(response);

                    var limitedVideos = new List<VideoResult>();

                    for (int i = 0; i < channel.LatestVideos.Count && i < Settings.HowManySubbedVideosToFetch; i++)
                    {
                        limitedVideos.Add(channel.LatestVideos[i]);
                    }

                    return limitedVideos;
                }
                catch (Exception ex)
                {

                    System.Diagnostics.Debug.WriteLine("Error fetching channel data from URL: " + apiUrl);
                    System.Diagnostics.Debug.WriteLine("Error message: " + ex.Message);
                    return new List<VideoResult>();
                }
            }
        }

        public static async Task<List<VideoResult>> GetSubscribedChannelsVideosAsync()
        {
            var videos = new List<VideoResult>();

            if (SubscriptionManager.SubscribedAuthors == null || SubscriptionManager.SubscribedAuthors.Count == 0)
            {

                var dialog = new MessageDialog("Subscribe to a channel!");
                await dialog.ShowAsync();
                return videos;
            }

            foreach (var author in SubscriptionManager.SubscribedAuthors)
            {
                var latestVideos = await FetchLatestVideosAsync(author);
                videos.AddRange(latestVideos);
            }

            videos.Sort(CompareVideosByPublished);

            return videos;
        }

        private async void LoadRecommendedVideos()
        {
            if (_videoHistory == null || _videoHistory.Count == 0)
            {
                var dialog = new MessageDialog("Watch a video to get recommended!");
                await dialog.ShowAsync();
                return;
            }

            var recommendedVideosList = new List<VideoResult>();
            var addedVideoIds = new HashSet<string>();
            var historyVideoChannelIds = new HashSet<string>();

            foreach (var video in _videoHistory)
            {
                if (!string.IsNullOrEmpty(video.AuthorId))
                {
                    historyVideoChannelIds.Add(video.AuthorId);
                }
            }

            var tasks = new List<Task>();

            foreach (var video in _videoHistory)
            {
                string videoId = video.VideoId;
                string videoUrl = Settings.InvidiousInstance + "/api/v1/videos/" + videoId;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            var response = await client.GetStringAsync(videoUrl);
                            var videoData = JsonConvert.DeserializeObject<VideoData>(response);

                            if (videoData != null && videoData.RecommendedVideos != null)
                            {
                                var tempRecommendedVideos = new List<VideoResult>();

                                foreach (var recommendedVideo in videoData.RecommendedVideos)
                                {
                                    if (!historyVideoChannelIds.Contains(recommendedVideo.AuthorId) &&
                                        !addedVideoIds.Contains(recommendedVideo.VideoId) &&
                                        tempRecommendedVideos.Count < 2)
                                    {
                                        tempRecommendedVideos.Add(recommendedVideo);
                                        addedVideoIds.Add(recommendedVideo.VideoId);
                                    }
                                }

                                lock (recommendedVideosList)
                                {
                                    recommendedVideosList.AddRange(tempRecommendedVideos);
                                }

                                if (recommendedVideosList.Count >= Settings.RecommendVideoLimit)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error fetching recommended videos from {videoUrl}: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            if (recommendedVideosList.Count > 0)
            {
                RecommendedListView.ItemsSource = recommendedVideosList;
                RecTextNoHistroy.Text = string.Empty;
            }
            else
            {
                RecommendedListView.ItemsSource = null;
                RecTextNoHistroy.Text = "No recommendations available. Watch more videos to get recommendations!";
            }
        }

        private static int CompareVideosByPublished(VideoResult v1, VideoResult v2)
        {
            return v2.Published.CompareTo(v1.Published);
        }

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer)
            {
                return (ScrollViewer)depObj;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
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
                case "history":
                    LoadHistory();
                    break;
                case "subscriptions":
                    LoadSubscribedChannelsVideos();
                    break;
                case "recommended":
                    LoadRecommendedVideos();
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

        public static void SaveHistoryTrend(TrendingVideo video)
        {

            var videoResult = new VideoResult
            {
                VideoId = video.VideoId,
                Title = video.Title,
                Author = video.Author,
                

                VideoThumbnails = new List<VideoThumbnail>
        {
            new VideoThumbnail
            {
                Url = video.ThumbnailUrl
            }
        }
            };

            AddVideoToHistory(videoResult);
        }

        public static void AddVideoToHistory(VideoResult video)
        {

            if (_videoHistory.Count >= Settings.MaxHistorySize)
            {
                _videoHistory.RemoveAt(0);
            }

            _videoHistory.Add(video);

            try
            {

                string jsonHistory = JsonConvert.SerializeObject(_videoHistory);

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
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

        private void ClearSubcriptions_Click(object sender, RoutedEventArgs e)
        {
            SubscriptionManager.ClearSubscriptions();
            SubscriptionManager.SaveSubscriptions();

        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            ClearHistory();
        }

        private void ClearSavedSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.ClearSettings();
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
         
            }

        }

        private void ImageButton_Click2(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            VideoResult video = button != null ? button.DataContext as VideoResult : null;

            if (video != null)
            {
                Frame.Navigate(typeof(VideoPage), video.VideoId);
            
            }
        }

        private void TimeoffToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                Settings.ScreenTimeOut = toggleSwitch.IsOn;
                InitializeDisplayRequest();
            }
        }

        private void Sponserblock_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                Settings.isSponserBlock = toggleSwitch.IsOn;
                InitializeDisplayRequest();
            }
        }

        private void ShowSponserSkip_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                Settings.showSponserSkipMessage = toggleSwitch.IsOn;
                InitializeDisplayRequest();
            }
        }

        private void DoulbeTapToSkip_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                Settings.doubleTapToSkip = toggleSwitch.IsOn;
                InitializeDisplayRequest();
            }
        }

        private void UseFormatScreenToDownloadVideos_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                Settings.useFormatStreamForDownloads = toggleSwitch.IsOn;
                InitializeDisplayRequest();
            }
        }

        private void SetInvidiousInstanceButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.InvidiousInstance = InvidiousInstanceTextBox.Text;
        }

        private void SetInvidiousInstanceCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.InvidiousInstanceComments = InvidiousInstanceCommentsTextBox.Text;
        }

        private void SetReturnDislikesTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.ReturnDislikeInstance = ReturnDislikesTextBox.Text;
        }

        private void SetSponserblockTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.SponserBlockInstance = SponserblockTextBox.Text;
        }

        private void SetSubbedVideos_Click(object sender, RoutedEventArgs e)
        {
            int value;
            if (int.TryParse(SubbedVideosTextBox.Text, out value) && value >= 1 && value <= 100)
            {
                Settings.HowManySubbedVideosToFetch = value;

                ShowDialog("Subscribed videos limit updated successfully.");
            }
            else
            {
                ShowDialog("Please enter a valid number between 1 and 100 for subscribed videos.");
                SubbedVideosTextBox.Text = string.Empty;
            }
        }

        private void SetRecommendedVideos_Click(object sender, RoutedEventArgs e)
        {
            int value;
            if (int.TryParse(RecommendedVideosTextBox.Text, out value) && value >= 1 && value <= 100)
            {
                Settings.RecommendVideoLimit = value;

                ShowDialog("Recommended videos limit updated successfully.");
            }
            else
            {
                ShowDialog("Please enter a valid number between 1 and 100 for recommended videos.");
                RecommendedVideosTextBox.Text = string.Empty;
            }
        }

        private async void ShowDialog(string message)
        {
            var dialog = new Windows.UI.Popups.MessageDialog(message);
            await dialog.ShowAsync();
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            string version = Settings.version;
            string aboutMessage = $"MangoTube - Version {version}\n\n" +
                                  "MangoTube is a YouTube client for Windows Phone 8.1 and 10 devices.\n\n" +
                                  "Credits:\n\n" +
                                  "NCP3.0 (Developer)\n\n" +
                                  "Return Dislike API (Dislikes)\n\n" +
                                  "Invidious (For their great APIs!)\n\n" +
                                  "SponsorBlock (For Sponsorblock)!\n\n" +
                                  "Windows Phone Community For Their Support!";

            var dialog = new MessageDialog(aboutMessage, "About");
            await dialog.ShowAsync();
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

                    Settings._selectedQuality = qualityTag;

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

        private void SearchButtonToPage_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SearchPage));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

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
        public List<VideoThumbnail> VideoThumbnails { get; set; } = new List<VideoThumbnail>();
        public Thumbnail Thumbnail { get; set; }
        public int LengthSeconds { get; set; }
        public string authorId { get; set; }
        public string viewCount { get; set; }
        
        public string genre { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();



        public string LengthFormatted
        {
            get
            {
                if (LengthSeconds < 0)
                {
                    return null; 
                }

                TimeSpan timeSpan = TimeSpan.FromSeconds(LengthSeconds);

                if (timeSpan.TotalMinutes < 100)
                {
                    return timeSpan.ToString(@"mm\:ss");
                }
                else
                {
                    return timeSpan.ToString(@"hh\:mm\:ss");
                }
            }
        }

    }

    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value != null && value is string)
            {
                string str = (string)value;

                return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class ChannelTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is string && (string)value == "channel")
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }


    public class ChannelTypeToVisibilityConverterOppsite : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is string && (string)value == "channel")
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            if (value is string && !string.IsNullOrEmpty(value.ToString()))
            {

                return new BitmapImage(new Uri($"https:{value}", UriKind.Absolute));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
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

    public class VideoData
    {
        [JsonProperty("recommendedVideos")]
        public List<VideoResult> RecommendedVideos { get; set; }
    }

    public class VideoThumbnail
    {
        public string Quality { get; set; }
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class AuthorThumbnail
    {
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
        public string Type { get; set; }

        public Thumbnail Thumbnail { get; set; }
        public List<VideoThumbnail> VideoThumbnails { get; set; } = new List<VideoThumbnail>();

        public List<AuthorThumbnail> AuthorThumbnails { get; set; } = new List<AuthorThumbnail>();

        public string FirstAuthorThumbnailUrl
        {
            get
            {
                return AuthorThumbnails.Count > 0 ? AuthorThumbnails[0].Url : null;
            }
        }


        public bool IsChannel => Type == "channel";

        public bool IsFavorite { get; set; }
        public long Published { get; set; }
        public string AuthorId { get; set; }
        public int LengthSeconds { get; set; }
        public int SubCount { get; set; }
        public string ChannelHandle { get; set; }

        public string FormattedSubCount
        {
            get
            {

                if (SubCount >= 1000000)
                {
                    double millionCount = SubCount / 1000000.0;
                    return String.Format("{0:0.#}m Subscribers", millionCount);
                }
                else if (SubCount >= 1000)
                {
                    double thousandCount = SubCount / 1000.0;
                    return String.Format("{0:0.#}k Subscribers", thousandCount);
                }
                else
                {
                    return String.Format("{0} Subscribers", SubCount);
                }
            }
        }

        public string LengthFormatted
        {
            get
            {
                TimeSpan timeSpan = TimeSpan.FromSeconds(LengthSeconds);

                if (timeSpan.TotalMinutes < 100)
                {

                    return timeSpan.ToString(@"mm\:ss");
                }
                else
                {

                    return timeSpan.ToString(@"hh\:mm\:ss");
                }
            }
        }

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