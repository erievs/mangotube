using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using Windows.UI.Notifications;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using System.Diagnostics;
using Windows.UI.Xaml.Documents;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Input;
using Windows.UI;

namespace ValleyTube
{
    public sealed partial class VideoPage : Page
    {
        private string videoId;
        private string aurthorId;
        private string continuationToken;
        private DispatcherTimer syncTimer;
        private bool isSyncing = false;
        private VideoResult nextVideo;
        private List<SponsorBlockSegment> sponsorSegments;

        public VideoPage()
        {
            this.InitializeComponent();
            InitializeSyncTimer();
            SubscriptionManager.LoadSubscriptions();

        }

        private void InitializeSyncTimer()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromMilliseconds(200);
            syncTimer.Tick += SyncTimer_Tick;
        }

        private void SyncTimer_Tick(object sender, object e)
        {

            if (AudioPlayer.Source != null)
            {
                SyncAudioWithVideo();
            }

            if (Settings.isSponserBlock)
            {
                SkipSponsorSegments();
            }

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string parameter = e.Parameter as string;
            if (parameter != null)
            {
                videoId = parameter;
                System.Diagnostics.Debug.WriteLine("Navigated to VideoPage with videoId: " + videoId);

                LoadLikesDislikesData(videoId);
                LoadComments();

                await LoadVideoDetails();
                await LoadRelatedVideos();

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error: Parameter is not a valid string.");
            }
        }

        public async void LoadLikesDislikesData(string videoId)
        {
            var url = $"{Settings.ReturnDislikeInstance}/Votes?videoId={videoId}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(url);

                    if (response.StartsWith("<"))
                    {

                        System.Diagnostics.Debug.WriteLine("Received HTML instead of JSON. Response: " + response);
                        LikesTextBlock.Text = "[N/A] Likes";
                        DislikesTextBlock.Text = "[N/A] Dislikes";
                        return;
                    }

                    var jsonData = JObject.Parse(response);

                    var likes = (int)jsonData["likes"];
                    var dislikes = (int)jsonData["dislikes"];

                    LikesTextBlock.Text = $"{likes:N0} Likes - ";
                    DislikesTextBlock.Text = $"{dislikes:N0} Dislikes";
                }
                catch (HttpRequestException ex)
                {

                    LikesTextBlock.Text = "[N/A] Likes";
                    DislikesTextBlock.Text = "[N/A] Dislikes";
                    System.Diagnostics.Debug.WriteLine($"Error fetching data: {ex.Message}");
                }
                catch (JsonReaderException ex)
                {

                    LikesTextBlock.Text = "[N/A] Likes";
                    DislikesTextBlock.Text = "[N/A] Dislikes";
                    System.Diagnostics.Debug.WriteLine($"Error parsing JSON: {ex.Message}");
                }
                catch (Exception ex)
                {

                    LikesTextBlock.Text = "[N/A] Likes";
                    DislikesTextBlock.Text = "[N/A] Dislikes";
                    System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        private async Task<List<SponsorBlockSegment>> GetSponsorSegments(string videoId)
        {
            string sponsorBlockUrl = Settings.SponserBlockInstance + "/api/skipSegments?videoID=" + videoId;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(sponsorBlockUrl);
                    var segments = JsonConvert.DeserializeObject<List<SponsorBlockSegment>>(response);
                    foreach (var segment in segments)
                    {
                        System.Diagnostics.Debug.WriteLine($"SponsorBlock Segment: Start - {segment.Segment[0]}, End - {segment.Segment[1]}, Category - {segment.Category}");
                    }

                    return segments;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching SponsorBlock segments: " + ex.Message);
                    return new List<SponsorBlockSegment>();
                }
            }
        }

        private async Task LoadVideoDetails()
        {
            string videoUrl = Settings.InvidiousInstance + "/api/v1/videos/" + videoId;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videoUrl);
                    var video = JsonConvert.DeserializeObject<VideoDetail>(response);

                    VideoTitle.Text = video.Title;
                    aurthorId = video.authorId;

                    Uri videoUri = null;
                    Uri audioUri = null;

                    var selectedQuality = Settings.SelectedQuality;
                    System.Diagnostics.Debug.WriteLine("Selected Quality: " + selectedQuality);

                    sponsorSegments = await GetSponsorSegments(videoId);

                    if (selectedQuality == "360p-direct")
                    {
                        string videoUrlString = string.Format(Settings.InvidiousInstance + "/latest_version?id={0}&itag=18&local=true", video.VideoId);
                        videoUri = new Uri(videoUrlString);
                        System.Diagnostics.Debug.WriteLine("Using Direct Video URL: " + videoUri.AbsoluteUri);
                    }

                    else if (selectedQuality == "360p-format-stream")
                    {

                        var adaptiveFormat = video.formatStreams.FirstOrDefault(f => f.qualityLabel == "360p");

                        if (adaptiveFormat != null)
                        {
                            videoUri = new Uri(adaptiveFormat.url);
                            System.Diagnostics.Debug.WriteLine("Using Adaptive Video URL: " + videoUri.AbsoluteUri);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No 360p-adaptive format found.");
                        }
                    }
                    else
                    {
                        videoUri = GetBestVideoUri(video.adaptiveFormats, selectedQuality, out audioUri);
                        System.Diagnostics.Debug.WriteLine("Best Video URI: " + (videoUri != null ? videoUri.AbsoluteUri : "Not Found"));

                        if (audioUri != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Audio URI: " + audioUri.AbsoluteUri);
                        }
                    }

                    VideoPlayer.MediaFailed += (sender, args) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Media failed to play. Error: " + args.ErrorMessage);
                    };

                    if (videoUri != null)
                    {
                        VideoPlayer.Source = videoUri;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid video URL found.");
                    }

                    if (audioUri != null && Settings.SelectedQuality != "360p-direct")
                    {
                        AudioPlayer.Source = audioUri;
                        AudioPlayer.Play();
                    }

                    long unixTimestamp = long.Parse(video.published);
                    long views = long.Parse(video.viewCount);

                    string formattedDate = Utils.ConvertUnixTimestampToRelativeTime(unixTimestamp);
                    string formattedViews = Utils.AddCommasToNumber(views);
                    string formattedTags = Utils.ConvertKeywordsToCommaSeparated(video.Keywords);

                    DateTextBlock.Text = formattedDate;
                    AuthorTextBlock.Text = video.author + "  -  ";
                    ViewTextBlock.Text = " - " + formattedViews + " views";

                    SetDescriptionText(video.Description);
                    GenreTextBlock.Text = "Genre: " + video.genre;
                    TagsTextBlock.Text = "Tags: " + formattedTags;

                    InitializeSubscriptionButton(video.VideoId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading video details: " + ex.Message);
                }
            }
        }

        private void SetDescriptionText(string description)
        {

            DescriptionRichTextBlock.Blocks.Clear();

            string urlPattern = @"(http|https)://[^\s]+";
            MatchCollection matches = Regex.Matches(description, urlPattern);

            var paragraph = new Paragraph();

            int lastIndex = 0;
            foreach (Match match in matches)
            {

                if (match.Index > lastIndex)
                {
                    var run = new Run();
                    run.Text = description.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Inlines.Add(run);
                }

                var hyperlink = new Hyperlink
                {
                    Foreground = new SolidColorBrush(Windows.UI.Colors.LightBlue)
                };

                var linkRun = new Run();
                linkRun.Text = match.Value;
                hyperlink.Inlines.Add(linkRun);

                hyperlink.Click += (s, e) =>
                {

                    Windows.System.Launcher.LaunchUriAsync(new Uri(match.Value));
                };

                paragraph.Inlines.Add(hyperlink);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < description.Length)
            {
                var remainingRun = new Run();
                remainingRun.Text = description.Substring(lastIndex);
                paragraph.Inlines.Add(remainingRun);
            }

            DescriptionRichTextBlock.Blocks.Add(paragraph);
        }

        public class QualityRange
        {
            public int MinQuality { get; set; }
            public int MaxQuality { get; set; }

            public QualityRange(int min, int max)
            {
                MinQuality = min;
                MaxQuality = max;
            }
        }

        private Uri GetBestVideoUri(IEnumerable<VideoFormat> formats, string selectedQuality, out Uri audioUri)
        {
            audioUri = null;

            if (formats == null)
            {
                System.Diagnostics.Debug.WriteLine("No video formats available.");
                return null;
            }

            var qualityRanges = new Dictionary<string, QualityRange>();
            qualityRanges.Add("144", new QualityRange(100, 199));
            qualityRanges.Add("240", new QualityRange(200, 299));
            qualityRanges.Add("360", new QualityRange(300, 399));
            qualityRanges.Add("480", new QualityRange(400, 599));
            qualityRanges.Add("720", new QualityRange(600, 799));
            qualityRanges.Add("1080", new QualityRange(800, 1100));

            if (!qualityRanges.ContainsKey(selectedQuality))
            {
                System.Diagnostics.Debug.WriteLine("Selected quality '" + selectedQuality + "' is not defined in quality ranges.");
                return null;
            }

            int minQuality = qualityRanges[selectedQuality].MinQuality;
            int maxQuality = qualityRanges[selectedQuality].MaxQuality;

            VideoFormat bestFormat = null;

            foreach (VideoFormat format in formats)
            {
                if (format.encoding == "h264")
                {
                    int quality = ParseQuality(format.qualityLabel);
                    if (quality >= minQuality && quality <= maxQuality)
                    {
                        if (bestFormat == null || ParseQuality(format.qualityLabel) > ParseQuality(bestFormat.qualityLabel))
                        {
                            bestFormat = format;
                        }
                    }
                }
            }

            if (bestFormat != null)
            {
                System.Diagnostics.Debug.WriteLine("Selected video quality: " + bestFormat.qualityLabel);
                System.Diagnostics.Debug.WriteLine("Selected video itag: " + bestFormat.itag);
                var videoUri = new Uri(bestFormat.url);

                foreach (VideoFormat format in formats)
                {
                    if (format.itag == "140")
                    {
                        audioUri = new Uri(format.url);
                        System.Diagnostics.Debug.WriteLine("Selected audio track URL: " + audioUri.AbsoluteUri);
                        break;
                    }
                }

                return videoUri;
            }

            System.Diagnostics.Debug.WriteLine("No suitable video format found.");
            return null;
        }

        private bool IsQualityInRange(string qualityLabel, int minQuality, int maxQuality)
        {
            int parsedQuality = ParseQuality(qualityLabel);
            return parsedQuality >= minQuality && parsedQuality <= maxQuality;
        }

        private int ParseQuality(string qualityLabel)
        {

            if (qualityLabel.EndsWith("p"))
            {
                qualityLabel = qualityLabel.Substring(0, qualityLabel.Length - 1);
            }

            string numericPart = "";
            foreach (char c in qualityLabel)
            {
                if (char.IsDigit(c))
                {
                    numericPart += c;
                }
            }

            int quality;
            if (int.TryParse(numericPart, out quality))
            {
                return quality;
            }

            return 0;
        }

        private void LoadComments(string sortBy = "top", string source = "youtube")
        {
            string commentsUrl = Settings.InvidiousInstanceComments + "/api/v1/comments/" + videoId +
                                "?sort_by=" + sortBy +
                                "&source=" + source;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                commentsUrl += "&continuation=" + continuationToken;
            }

            System.Diagnostics.Debug.WriteLine("Request URL: " + commentsUrl);

            using (var httpClient = new HttpClient())
            {
                try
                {

                    var responseTask = httpClient.GetAsync(commentsUrl);
                    responseTask.Wait();
                    var response = responseTask.Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseDataTask = response.Content.ReadAsStringAsync();
                        responseDataTask.Wait();
                        var responseData = responseDataTask.Result;

                        var commentsData = JsonConvert.DeserializeObject<CommentsResponse>(responseData);

                        if (commentsData != null && commentsData.comments != null)
                        {
                            if (continuationToken == null)
                            {
                                CommentsListView.ItemsSource = commentsData.comments;
                            }
                            else
                            {
                                var currentItems = CommentsListView.ItemsSource as List<Comment>;
                                if (currentItems != null)
                                {
                                    currentItems.AddRange(commentsData.comments);
                                    CommentsListView.ItemsSource = null;
                                    CommentsListView.ItemsSource = currentItems;
                                }
                            }

                            continuationToken = commentsData.continuation;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No comments found.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format("Error loading comments: {0} - {1}", response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading comments: " + ex.Message);
                }
            }
        }

        private void CommentsScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null)
            {
                System.Diagnostics.Debug.WriteLine($"ScrollViewer VerticalOffset: {scrollViewer.VerticalOffset}");
                System.Diagnostics.Debug.WriteLine($"ScrollViewer ScrollableHeight: {scrollViewer.ScrollableHeight}");

                double threshold = scrollViewer.ScrollableHeight * 0.1;

                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - threshold)
                {
                    System.Diagnostics.Debug.WriteLine("Reached the threshold, loading more comments.");
                    LoadComments();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Not close enough to the bottom to load more comments.");
                }
            }
        }

        private async Task LoadRelatedVideos()
        {
            string relatedVideosUrl = Settings.InvidiousInstance + "/api/v1/videos/" + videoId;

            System.Diagnostics.Debug.WriteLine("Requesting URL: " + relatedVideosUrl);

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(relatedVideosUrl);
                    var videoData = JsonConvert.DeserializeObject<VideoDetail>(response);

                    if (videoData == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to deserialize video data.");
                        return;
                    }

                    if (videoData.recommendedVideos != null && videoData.recommendedVideos.Any())
                    {
                        RelatedVideosListView.ItemsSource = videoData.recommendedVideos;

                        nextVideo = videoData.recommendedVideos.First();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No recommended videos found.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading related videos: " + ex.Message);
                }
            }
        }

        private int GetQuality(string qualityLabel)
        {
            if (string.IsNullOrEmpty(qualityLabel))
            {
                return 0;
            }

            if (qualityLabel.EndsWith("p"))
            {
                string qualityWithoutP = qualityLabel.Substring(0, qualityLabel.Length - 1);
                int quality;
                if (int.TryParse(qualityWithoutP, out quality))
                {
                    return quality;
                }
            }

            return 0;
        }

        private void GoToNextVideo()
        {
            if (nextVideo != null)
            {
                System.Diagnostics.Debug.WriteLine("Navigating to next video with ID: " + nextVideo.VideoId);
                Frame.Navigate(typeof(VideoPage), nextVideo.VideoId);
                MainPage.SaveHistory(nextVideo);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No next video available.");
            }
        }

        private async void SkipSponsorSegments()
        {
            if (sponsorSegments == null || !sponsorSegments.Any())
            {
                SponsorSkipMessageTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            var currentPosition = VideoPlayer.Position.TotalSeconds;
            int skippedSegmentsCount = 0;

            foreach (var segment in sponsorSegments)
            {
                if (currentPosition >= segment.Segment[0] && currentPosition <= segment.Segment[1])
                {
                    TimeSpan skipToTime = TimeSpan.FromSeconds(segment.Segment[1]);

                    System.Diagnostics.Debug.WriteLine($"Skipping sponsor segment from {segment.Segment[0]} to {segment.Segment[1]}");

                    VideoPlayer.Position = skipToTime;

                    if (AudioPlayer.Source != null)
                    {
                        AudioPlayer.Position = skipToTime;
                    }

                    skippedSegmentsCount++;

                    if (Settings.showSponserSkipMessage)
                    {
                        ShowToastNotification("Skipped sponsor segment!");
                    }

                    break;
                }
            }
        }

        private void VideoPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {

            if (isSyncing) return;

            isSyncing = true;
            try
            {
                switch (VideoPlayer.CurrentState)
                {
                    case MediaElementState.Playing:
                        if (!syncTimer.IsEnabled)
                        {
                            syncTimer.Start();
                        }

                        break;

                    case MediaElementState.Paused:
                        syncTimer.Stop();
                        if (AudioPlayer.CurrentState == MediaElementState.Playing)
                        {
                            AudioPlayer.Pause();
                        }
                        break;

                    case MediaElementState.Stopped:
                        syncTimer.Stop();
                        AudioPlayer.Stop();
                        break;
                }
            }
            finally
            {
                isSyncing = false;
            }
        }

        private void SyncAudioWithVideo()
        {
            if (AudioPlayer.Source == null)
            {
                return;
            }

            double positionDifference = Math.Abs((VideoPlayer.Position - AudioPlayer.Position).TotalSeconds);

            if (positionDifference > 0.5)
            {
                AudioPlayer.Position = VideoPlayer.Position;
                System.Diagnostics.Debug.WriteLine($"Sync correction applied. Audio Player position adjusted to {AudioPlayer.Position} to match Video Player.");
            }

            if (AudioPlayer.CurrentState != MediaElementState.Playing)
            {
                AudioPlayer.Play();
                System.Diagnostics.Debug.WriteLine("Audio Player started playing.");
            }
        }

        private async Task DownloadVideo(string videoId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting download for video ID: {videoId}");

                string fileName = $"youtubevideo_{videoId}.mp4";
                var storageFolder = Windows.Storage.KnownFolders.VideosLibrary;
                var file = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                System.Diagnostics.Debug.WriteLine($"File created: {file.Path}");

                Uri videoUri = null;

                bool useFormatStreamForDownloads = Settings.useFormatStreamForDownloads;

                if (useFormatStreamForDownloads)
                {

                    string videoUrl = Settings.InvidiousInstance + "/api/v1/videos/" + videoId;
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetStringAsync(videoUrl);
                        var video = JsonConvert.DeserializeObject<VideoDetail>(response);

                        var formatStream = video.formatStreams.FirstOrDefault(f => f.qualityLabel == "360p");

                        if (formatStream != null)
                        {
                            videoUri = new Uri(formatStream.url);
                            System.Diagnostics.Debug.WriteLine($"Using FormatStream URL: {videoUri.AbsoluteUri}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No 360p format found in formatStreams.");
                            return;
                        }
                    }
                }
                else
                {

                    string videoUrlString = string.Format(Settings.InvidiousInstance + "/latest_version?id={0}&itag=18&local=true", videoId);
                    videoUri = new Uri(videoUrlString);
                    System.Diagnostics.Debug.WriteLine($"Using Direct Video URL: {videoUri.AbsoluteUri}");
                }

                using (var httpClient = new HttpClient())
                {
                    DownloadProgressLabel.Visibility = Visibility.Visible;
                    DownloadProgressBar.Visibility = Visibility.Visible;

                    var response = await httpClient.GetAsync(videoUri, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        long totalBytes = response.Content.Headers.ContentLength ?? 0;
                        using (var fileStream = await file.OpenStreamForWriteAsync())
                        {
                            var stream = await response.Content.ReadAsStreamAsync();
                            byte[] buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (totalBytes > 0)
                                {
                                    double progressPercentage = (double)totalRead / totalBytes * 100;
                                    DownloadProgressBar.Value = progressPercentage;

                                    System.Diagnostics.Debug.WriteLine($"Download progress: {progressPercentage:F2}%");
                                }
                            }
                        }

                        ShowToastNotification($"Download completed: {file.Path}");
                        System.Diagnostics.Debug.WriteLine($"Download completed: {file.Path}");
                    }
                    else
                    {
                        ShowToastNotification($"Error downloading video: {response.ReasonPhrase}");
                        System.Diagnostics.Debug.WriteLine($"Error downloading video: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error downloading video: " + ex.Message);
                ShowToastNotification($"Download failed: {ex.Message}");
            }
            finally
            {
                DownloadProgressLabel.Visibility = Visibility.Collapsed;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                DownloadProgressBar.Value = 0;
            }
        }

        private async void ShowToastNotification(string message)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    var dialog = new MessageDialog(message);
                    await dialog.ShowAsync();
                }
                catch (UnauthorizedAccessException ex)
                {

                    Debug.WriteLine($"Access denied: {ex.Message}");
                }
                catch (Exception ex)
                {

                    Debug.WriteLine($"Error showing toast notification: {ex.Message}");
                }
            });
        }

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Debug.WriteLine("AudioPlayer media opened.");
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (Settings.IsAutoplayEnabled)
            {
                GoToNextVideo();
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Debug.WriteLine("VideoPlayer media opened.");
        }

        private void VideoPlayer_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (Settings.doubleTapToSkip)
            {
                GoToNextVideo();
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (Settings.IsAutoplayEnabled)
            {
                GoToNextVideo();
            }
        }

        private void OnResuming(object sender, object e)
        {
            if (VideoPlayer.Source != null)
            {
                SyncAudioWithVideo();
                System.Diagnostics.Debug.WriteLine("App resumed, video playing resumed.");
            }
        }

        private void LoadMoreComments_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                LoadComments();
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button != null)
            {
                VideoResult video = button.DataContext as VideoResult;
                if (video != null)
                {
                    System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                    Frame.Navigate(typeof(VideoPage), video.VideoId);
                    MainPage.SaveHistory(video);
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(videoId))
            {
                await DownloadVideo(videoId);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Video ID is null or empty.");
            }
        }

        private void AuthorButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Navigating to channel with ID: " + aurthorId);
            Frame.Navigate(typeof(ChannelPage), aurthorId);
        }

        private void Button_Click_1(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Image image = sender as Image;
            if (image != null)
            {
                VideoResult video = image.DataContext as VideoResult;
                if (video != null)
                {
                    System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                    Frame.Navigate(typeof(VideoPage), video.VideoId);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }

        private async Task<string> GetCurrentAuthorAsync(string videoId)
        {
            string apiUrl = Settings.InvidiousInstance + "/api/v1/videos/" + videoId;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var videoData = JsonConvert.DeserializeObject<VideoResult>(response);

                    return videoData.AuthorId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching author: " + ex.Message);
                    return null;
                }
            }
        }

        private async void InitializeSubscriptionButton(String videoId)
        {
            try
            {

                System.Diagnostics.Debug.WriteLine("videoId is " + videoId);

                string authorId = await GetCurrentAuthorAsync(videoId);

                System.Diagnostics.Debug.WriteLine("Retrieved authorId is " + authorId);

                if (!string.IsNullOrEmpty(authorId))
                {
                    if (SubscriptionManager.IsSubscribed(authorId))
                    {
                        SubscribeButton.Content = "Unsubscribe";
                    }
                    else
                    {
                        SubscribeButton.Content = "Subscribe";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to retrieve authorId.");
                    SubscribeButton.Content = "Subscribe";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in InitializeSubscriptionButton: " + ex.Message);
            }
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            string authorId = await GetCurrentAuthorAsync(videoId);

            if (authorId != null)
            {
                if (SubscriptionManager.IsSubscribed(authorId))
                {

                    SubscriptionManager.Unsubscribe(authorId);
                    SubscribeButton.Content = "Subscribe";
                    SubscriptionManager.SaveSubscriptions();
                }
                else
                {

                    SubscriptionManager.Subscribe(authorId);
                    SubscribeButton.Content = "Unsubscribe";
                    SubscriptionManager.SaveSubscriptions();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to get author ID.");
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {

            DataPackage dataPackage = new DataPackage();
            string contentToShare = "Check out this awesome video!";

            dataPackage.SetText(contentToShare);

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

            dataTransferManager.DataRequested += (s, args) =>
            {

                args.Request.Data = dataPackage;

                args.Request.Data.Properties.Title = "Share Video";
                args.Request.Data.Properties.Description = "Sharing a video link!";
            };

            DataTransferManager.ShowShareUI();
        }

        private async void PinButton_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(videoId))
            {
                var dialog = new MessageDialog("Invalid video ID.");
                await dialog.ShowAsync();
                return;
            }

            VideoResult video = await FetchVideoDetailsAsync(videoId);
            if (video == null)
            {
                var dialog = new MessageDialog("Failed to fetch video details.");
                await dialog.ShowAsync();
                return;
            }

            string tileId = "VideoTile_" + videoId;

            string thumbnailUrl = string.Empty;
            if (video.VideoThumbnails != null && video.VideoThumbnails.Count > 0)
            {
                thumbnailUrl = video.VideoThumbnails[0].Url;
            }

            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                thumbnailUrl = "ms-appx:///Assets/Square150x150Logo.scale-240.png";
            }

            Uri thumbnailUri;
            try
            {
                thumbnailUri = new Uri(thumbnailUrl);
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Thumbnail URL is not valid: {ex.Message}");
                await dialog.ShowAsync();
                return;
            }

            var tile = new SecondaryTile(
                tileId,
                video.Title,
                "Video ID: " + videoId,
                new Uri($"ms-appx:///VideoPage.xaml?videoId={videoId}"),
                TileSize.Default)
            {
                VisualElements =
        {

            Square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-240.png"),
            Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Logo.scale-240.png"),
            BackgroundColor = Windows.UI.Colors.Blue,
            ForegroundText = ForegroundText.Dark
        }
            };

            bool isPinned = await tile.RequestCreateAsync();

            if (isPinned)
            {
                var dialog = new MessageDialog("Video pinned to Start!");
                await dialog.ShowAsync();
            }
            else
            {
                var dialog = new MessageDialog("Failed to pin the video.");
                await dialog.ShowAsync();
            }
        }

        private async Task<VideoResult> FetchVideoDetailsAsync(string videoId)
        {
            string apiUrl = Settings.InvidiousInstance + $"/api/v1/videos/{videoId}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var video = JsonConvert.DeserializeObject<VideoResult>(response);
                    return video;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error fetching video details: " + ex.Message);
                    return null;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}

public class CommentsResponse
{
    public int commentCount { get; set; }
    public string videoId { get; set; }
    public List<Comment> comments { get; set; }
    public string continuation { get; set; }
}

public class Comment
{
    public string authorId { get; set; }
    public string authorUrl { get; set; }
    public string author { get; set; }
    public bool verified { get; set; }
    public List<AuthorThumbnail> authorThumbnails { get; set; }
    public string content { get; set; }
    public string continuation { get; set; }
}

public class SponsorBlockSegment
{
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("actionType")]
    public string ActionType { get; set; }

    [JsonProperty("segment")]
    public List<double> Segment { get; set; }

    [JsonProperty("UUID")]
    public string UUID { get; set; }

    [JsonProperty("videoDuration")]
    public double VideoDuration { get; set; }

    [JsonProperty("locked")]
    public int Locked { get; set; }

    [JsonProperty("votes")]
    public int Votes { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class AuthorThumbnail
{
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}