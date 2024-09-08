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

namespace ValleyTube
{
    public sealed partial class VideoPage : Page
    {
        private string videoId;
        private string continuationToken;
        private DispatcherTimer syncTimer;
        private bool isSyncing = false;
        private VideoResult nextVideo;

        public VideoPage()
        {
            this.InitializeComponent();
            InitializeSyncTimer();
            SubscriptionManager.LoadSubscriptions();

        }

        private void InitializeSyncTimer()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromMilliseconds(100);
            syncTimer.Tick += SyncTimer_Tick;
        }


        private void SyncTimer_Tick(object sender, object e)
        {
            if (VideoPlayer.CurrentState == MediaElementState.Playing && AudioPlayer.Source != null)
            {
                TimeSpan videoPosition = VideoPlayer.Position;
                TimeSpan audioPosition = AudioPlayer.Position;

                TimeSpan threshold = TimeSpan.FromMilliseconds(200);

                if (Math.Abs((videoPosition - audioPosition).TotalMilliseconds) > threshold.TotalMilliseconds)
                {
                    AudioPlayer.Position = videoPosition;

                    System.Diagnostics.Debug.WriteLine(
                        String.Format("Sync correction applied. Video Position: {0}, Audio Position: {1}",
                        videoPosition,
                        audioPosition)
                    );
                }
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
                await LoadVideoDetails();
                await LoadRelatedVideos();
                await LoadComments();
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

                    LikesTextBlock.Text = $"{likes:N0} Likes";
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

                    Uri videoUri = null;
                    Uri audioUri = null;

                    var selectedQuality = Settings.SelectedQuality;
                    System.Diagnostics.Debug.WriteLine("Selected Quality: " + selectedQuality); 

                    if (selectedQuality == "360p-direct")
                    {
                        string videoUrlString = string.Format(Settings.InvidiousInstance + "/latest_version?id={0}&itag=18&local=true", video.VideoId);
                        videoUri = new Uri(videoUrlString);
                        System.Diagnostics.Debug.WriteLine("Using Direct Video URL: " + videoUri.AbsoluteUri); 
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
                    string formattedDate = Utils.ConvertUnixTimestampToRelativeTime(unixTimestamp);


                    DateTextBlock.Text = formattedDate;
                    AuthorTextBlock.Text = video.author + "  -  ";
                    DescriptionTextBlock.Text = video.Description;

                    InitializeSubscriptionButton(video.VideoId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading video details: " + ex.Message);
                }
            }
        }

        private Uri GetBestVideoUri(IEnumerable<VideoFormat> formats, string selectedQuality, out Uri audioUri)
        {
            audioUri = null;

            if (formats != null && formats.Any())
            {
                var bestFormat = formats
                     .Where(f => f.encoding == "h264" && f.qualityLabel.Contains(selectedQuality))
                    .OrderByDescending(f => ParseQuality(f.qualityLabel))
                    .FirstOrDefault();

                if (bestFormat != null)
                {
                    System.Diagnostics.Debug.WriteLine("Selected video quality: " + bestFormat.qualityLabel);
                    System.Diagnostics.Debug.WriteLine("Selected video itag: " + bestFormat.itag);
                    var videoUri = new Uri(bestFormat.url);

                    var audioFormat = formats.FirstOrDefault(f => f.itag == "140");
                    if (audioFormat != null)
                    {
                        audioUri = new Uri(audioFormat.url);
                        System.Diagnostics.Debug.WriteLine("Selected audio track URL: " + audioUri.AbsoluteUri);
                    }

                    return videoUri;
                }
            }

            System.Diagnostics.Debug.WriteLine("No suitable video format found.");
            return null;
        }

        private int GetBitrate(string bitrateLabel)
        {
            if (string.IsNullOrEmpty(bitrateLabel))
            {
                return 0;
            }

            int bitrate;
            if (int.TryParse(bitrateLabel, out bitrate))
            {
                return bitrate;
            }

            return 0;
        }

        private int ParseQuality(string qualityLabel)
        {
            if (string.IsNullOrEmpty(qualityLabel))
            {
                return 0;
            }

            if (qualityLabel.EndsWith("p"))
            {
                string numericPart = qualityLabel.Replace("p", "");

                int quality;
                if (int.TryParse(numericPart, out quality))
                {
                    return quality;
                }
            }

            if (string.Equals(qualityLabel, "HD", StringComparison.OrdinalIgnoreCase))
            {
                return 1080;
            }

            if (string.Equals(qualityLabel, "SD", StringComparison.OrdinalIgnoreCase))
            {
                return 480;
            }

            return 0;
        }

        private async Task LoadComments(string sortBy = "top", string source = "youtube")
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

        // ultra high tech code to make the audio and video player sync up somtimes

        private async void VideoPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            if (isSyncing) return;

            isSyncing = true;
            switch (VideoPlayer.CurrentState)
            {
                case MediaElementState.Playing:
                    if (AudioPlayer.Source != null)
                    {
                        AudioPlayer.Position = VideoPlayer.Position;
                        AudioPlayer.Play();
                    }
                    break;

                case MediaElementState.Paused:
                    AudioPlayer.Pause();
                    break;

                case MediaElementState.Stopped:
                    AudioPlayer.Stop();
                    break;
            }

            isSyncing = false;
        }


        // Buttons and stuff 

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // make it happy
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
            // make it happy
            System.Diagnostics.Debug.WriteLine("VideoPlayer media opened.");
        }

        private void VideoPlayer_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            GoToNextVideo();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (Settings.IsAutoplayEnabled)
            {
                GoToNextVideo();
            }
        }

        private async void LoadMoreComments_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                await LoadComments();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {

                Image image = button.Content as Image;
                if (image != null)
                {
                    VideoResult video = image.DataContext as VideoResult;
                    if (video != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                        Frame.Navigate(typeof(VideoPage), video.VideoId);
                        MainPage.SaveHistory(video);
                    }
                }
            }
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

                    return videoData.authorId;
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

        // just to make it happy


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // make it happy
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // make it happy
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

public class AuthorThumbnail
{
    public string url { get; set; }
    public int width { get; set; }
    public int height { get; set; }
}
