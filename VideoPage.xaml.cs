using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;

namespace ValleyTube
{
    public sealed partial class VideoPage : Page
    {
        private string videoId;

        public VideoPage()
        {
            this.InitializeComponent();
        }

        private string continuationToken;


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
            string videoUrl = "https://iv.ggtyler.dev/api/v1/videos/" + videoId;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videoUrl);
                    var video = JsonConvert.DeserializeObject<VideoDetail>(response);

                    VideoTitle.Text = video.Title;

                    Uri videoUri = null;

                    if (video.formatStreams != null && video.formatStreams.Count > 0)
                    {
                        var firstStream = video.formatStreams.FirstOrDefault();
                        if (firstStream != null)
                        {
                            videoUri = new Uri(firstStream.url);
                        }
                    }

                    if (videoUri != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Video URL: " + videoUri.AbsoluteUri);

                        VideoPlayer.Source = videoUri;
                        VideoPlayer.MediaFailed += (sender, args) =>
                        {
                            System.Diagnostics.Debug.WriteLine("Media failed to play. Error: " + args.ErrorMessage);
                        };
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid video URL found.");
                    }

                    DescriptionTextBlock.Text = video.Description;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading video details: " + ex.Message);
                }
            }
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

        private void Button_Click_2(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
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

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            Button button = sender as Button;
            if (button != null)
            {

                VideoResult video = button.DataContext as VideoResult;
                if (video != null)
                {

                    Frame.Navigate(typeof(VideoPage), video.VideoId);
                }
            }
        }


        private async void LoadMoreComments_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(continuationToken))
            {
                await LoadComments();
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
}