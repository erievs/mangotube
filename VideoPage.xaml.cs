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

namespace ValleyTube
{
    public sealed partial class VideoPage : Page
    {
        private string videoId;
        private string continuationToken;
        private DispatcherTimer syncTimer;
        private bool isSyncing = false;

        public VideoPage()
        {
            this.InitializeComponent();
            InitializeSyncTimer();
        }

        private void InitializeSyncTimer()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromMilliseconds(200); // Adjust the interval as needed
            syncTimer.Tick += SyncTimer_Tick;
        }

        private void SyncTimer_Tick(object sender, object e)
        {
            if (isSyncing) return;

            isSyncing = true;
            if (VideoPlayer.CurrentState == MediaElementState.Playing)
            {
                AudioPlayer.Position = VideoPlayer.Position;
            }
            isSyncing = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string parameter = e.Parameter as string;
            if (parameter != null)
            {
                videoId = parameter;
                System.Diagnostics.Debug.WriteLine("Navigated to VideoPage with videoId: " + videoId);

                await LoadVideoDetails();
                await LoadRelatedVideos();
                await LoadComments();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error: Parameter is not a valid string.");
            }
        }

        private async Task LoadVideoDetails()
        {
            string videoUrl = "https://invidious.nerdvpn.de/api/v1/videos/" + videoId;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videoUrl);
                    var video = JsonConvert.DeserializeObject<VideoDetail>(response);

                    VideoTitle.Text = video.Title;

                    Uri videoUri = null;
                    Uri audioUri = null;
                    Uri unusedAudioUri = null;

                    if (Settings.isDash)
                    {
                        videoUri = GetBestVideoUri(video.adaptiveFormats, out audioUri);
                        System.Diagnostics.Debug.WriteLine("Prefer High Quality Video May Lead To Deysnced Audio");
                    }
                    else
                    {
                        videoUri = GetBestVideoUri(video.formatStreams, out unusedAudioUri);
                    }

                    VideoPlayer.MediaFailed += (sender, args) =>
                    {
                        System.Diagnostics.Debug.WriteLine("Media failed to play. Error: " + args.ErrorMessage);
                    };

                    if (videoUri != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Video URL: " + videoUri.AbsoluteUri);
                        VideoPlayer.Source = videoUri;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid video URL found.");
                    }

                    if (audioUri != null && Settings.isDash)
                    {
                        System.Diagnostics.Debug.WriteLine("Audio URL: " + audioUri.AbsoluteUri);
                        AudioPlayer.Source = audioUri;
                        AudioPlayer.Play();
                    }

                    DescriptionTextBlock.Text = video.Description;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading video details: " + ex.Message);
                }
            }
        }

        private Uri GetBestVideoUri(IEnumerable<VideoFormat> formats, out Uri audioUri)
        {
            audioUri = null;

            if (formats != null && formats.Any())
            {
                var bestFormat = formats
                    .Where(f => f.itag != "140")
                    .OrderByDescending(f => ParseQuality(f.qualityLabel))
                    .FirstOrDefault();

                if (bestFormat != null)
                {
                    System.Diagnostics.Debug.WriteLine("Selected video quality: " + bestFormat.qualityLabel);
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
            string commentsUrl = "https://iv.ggtyler.dev/api/v1/comments/" + videoId +
                                 "?sort_by=" + sortBy +
                                 "&source=" + source;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                commentsUrl += "&continuation=" + continuationToken;
            }

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(commentsUrl);
                    var commentsData = JsonConvert.DeserializeObject<CommentsResponse>(response);

                    if (commentsData != null)
                    {
                        if (commentsData.comments != null)
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
                        }

                        continuationToken = commentsData.continuation;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No comments found.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading comments: " + ex.Message);
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

        private async Task LoadRelatedVideos()
        {
            string relatedVideosUrl = "https://iv.ggtyler.dev/api/v1/videos/" + videoId;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // make it happy
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

        private async void LoadMoreComments_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                await LoadComments();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // make it happy
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

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // make it happy
            System.Diagnostics.Debug.WriteLine("AudioPlayer media opened.");
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // make it happy
            System.Diagnostics.Debug.WriteLine("MediaElement media ended.");
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // make it happy
            System.Diagnostics.Debug.WriteLine("VideoPlayer media opened.");
        }


        // ultra high tech code to make the audio and video player sync up somtimes

        private void VideoPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
                if (isSyncing) return;

                isSyncing = true;
                switch (VideoPlayer.CurrentState)
                {
                    case MediaElementState.Playing:
                        AudioPlayer.Play();
                        syncTimer.Start();
                        break;

                    case MediaElementState.Paused:
                        AudioPlayer.Pause();
                        syncTimer.Stop();
                        break;

                    case MediaElementState.Stopped:
                        AudioPlayer.Stop();
                        syncTimer.Stop();
                        break;
                }
                isSyncing = false;
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