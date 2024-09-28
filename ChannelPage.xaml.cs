using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.Globalization;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Controls;

namespace ValleyTube
{
    public sealed partial class ChannelPage : Page
    {
        private string channelId;
        private string videoContinuation = string.Empty; 
        private string communityContinuation;
        private bool isLoadingMoreVideos = false;
        public ObservableCollection<VideoObjectAgain> Videos { get; set; } = new ObservableCollection<VideoObjectAgain>();

        public ChannelPage()
        {
            this.InitializeComponent();
            SubscriptionManager.LoadSubscriptions();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string parameter = e.Parameter as string;
            if (parameter != null)
            {
                channelId = parameter;
                LoadChannelData(); 
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error: Parameter is not a valid string.");
        
            }
        }


        private async Task LoadChannelData()
        {
            await LoadChannelDetails();
            await LoadChannelVideos();
            await LoadCommunityPosts();
        }

        private void InitializeSubscriptionButton(String channelId)
        {
            try
            {

                System.Diagnostics.Debug.WriteLine("channeId is " + channelId);

     

                System.Diagnostics.Debug.WriteLine("Retrieved authorId is " + channelId);

                if (!string.IsNullOrEmpty(channelId))
                {
                    if (SubscriptionManager.IsSubscribed(channelId))
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

        private async Task LoadChannelDetails()
        {
            string channelUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(channelUrl);
                    var channelData = JsonConvert.DeserializeObject<ChannelObject>(response);

                    if (channelData != null)
                    {
                        ChannelTitle.Text = channelData.author;
                        ChannelDescription.Text = channelData.description;
                        var bannerUrl = channelData.authorBanners.FirstOrDefault()?.Url ?? string.Empty;

                        SetBackgroundImage(bannerUrl);

                        ChannelStatsTextBlock.Text = $"Subscribers: {channelData.subCount}\nTotal Views: {channelData.totalViews}";
                        InitializeSubscriptionButton(channelId);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading channel details: " + ex.Message);
                }
            }
        }

        private void SetBackgroundImage(string bannerUrl)
        {

            var bitmapImage = new BitmapImage(new Uri(bannerUrl));

            BackgroundImage.Source = bitmapImage;
        }

        private async Task LoadChannelVideos()
        {
            string videosUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}/videos";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videosUrl);
                    System.Diagnostics.Debug.WriteLine("Raw JSON Response: " + response);

                    var videoResponse = JsonConvert.DeserializeObject<VideoResponse>(response);

                    if (videoResponse != null && videoResponse.videos != null)
                    {
                        Videos.Clear();
                        foreach (var video in videoResponse.videos)
                        {
                            Videos.Add(video);
                        }

                        videoContinuation = videoResponse.continuation;
                        System.Diagnostics.Debug.WriteLine("Continuation token updated: " + videoContinuation);

                        VideosListView.ItemsSource = Videos;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error: videoResponse is null or videos is null.");
                    }
                }
                catch (JsonSerializationException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine("JSON Serialization Error: " + jsonEx.Message);
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine("HTTP Request Error: " + httpEx.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading videos: " + ex.Message);
                }
            }
        }

        private void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
         
            if (channelId != null)
            {
                if (SubscriptionManager.IsSubscribed(channelId))
                {

                    SubscriptionManager.Unsubscribe(channelId);
                    SubscribeButton.Content = "Subscribe";
                    SubscriptionManager.SaveSubscriptions();
                }
                else
                {

                    SubscriptionManager.Subscribe(channelId);
                    SubscribeButton.Content = "Unsubscribe";
                    SubscriptionManager.SaveSubscriptions();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to get channelId.");
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null)
            {

                System.Diagnostics.Debug.WriteLine($"ScrollViewer VerticalOffset: {scrollViewer.VerticalOffset}");
                System.Diagnostics.Debug.WriteLine($"ScrollViewer ScrollableHeight: {scrollViewer.ScrollableHeight}");

                double threshold = scrollViewer.ScrollableHeight * 0.2;

                System.Diagnostics.Debug.WriteLine($"Threshold for loading more videos: {threshold}");

                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - threshold)
                {
                    System.Diagnostics.Debug.WriteLine("Reached the threshold, loading more videos.");
                    LoadMoreVideos();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Not close enough to the bottom to load more videos.");
                }
            }
        }

        private async void LoadMoreVideos()
        {
            if (string.IsNullOrEmpty(videoContinuation))
            {
                System.Diagnostics.Debug.WriteLine("No continuation token available to load more videos.");
                return;
            }

            string videosUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}/videos?continuation={videoContinuation}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videosUrl);
                    System.Diagnostics.Debug.WriteLine("Raw JSON Response for more videos: " + response);

                    var videoResponse = JsonConvert.DeserializeObject<VideoResponse>(response);

                    if (videoResponse != null && videoResponse.videos != null && videoResponse.videos.Count > 0)
                    {
                        foreach (var video in videoResponse.videos)
                        {
                            Videos.Add(video);
                        }

                        videoContinuation = videoResponse.continuation;
                        System.Diagnostics.Debug.WriteLine("Continuation token updated: " + videoContinuation);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No more videos to load or videoResponse is null.");
                    }
                }
                catch (JsonSerializationException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine("JSON Serialization Error: " + jsonEx.Message);
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine("HTTP Request Error: " + httpEx.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading more videos: " + ex.Message);
                }
            }
        }

        private async Task LoadCommunityPosts()
        {
            string communityUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}/community";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(communityUrl);
                    var communityData = JsonConvert.DeserializeObject<CommunityResponse>(response);

                    if (communityData != null && communityData.comments != null)
                    {
                        CommunityListView.ItemsSource = communityData.comments;
                        communityContinuation = communityData.continuation;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error: communityData is null or comments is null.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading community posts: " + ex.Message);
                }
            }
        }

        private async void LoadMoreCommunityPosts()
        {
            if (string.IsNullOrEmpty(communityContinuation))
            {
                System.Diagnostics.Debug.WriteLine("No continuation token available to load more community posts.");
                return;
            }

            string communityUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}/community?continuation={communityContinuation}";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(communityUrl);
                    System.Diagnostics.Debug.WriteLine("Raw JSON Response for more community posts: " + response);

                    var communityData = JsonConvert.DeserializeObject<CommunityResponse>(response);

                    if (communityData != null && communityData.comments != null && communityData.comments.Count > 0)
                    {

                        foreach (var comment in communityData.comments)
                        {
                            ((List<ChannelComment>)CommunityListView.ItemsSource).Add(comment);
                        }

                        communityContinuation = communityData.continuation;
                        System.Diagnostics.Debug.WriteLine("Continuation token updated for community posts: " + communityContinuation);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No more community posts to load or communityData is null.");
                    }
                }
                catch (JsonSerializationException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine("JSON Serialization Error: " + jsonEx.Message);
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine("HTTP Request Error: " + httpEx.Message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading more community posts: " + ex.Message);
                }
            }
        }

        public static void SaveHistoryVideoObject(VideoObjectAgain video)
        {

            var videoResult = new VideoResult
            {
                VideoId = video.videoId,
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

            MainPage.AddVideoToHistory(videoResult);
        }

        private void CommunityScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null)
            {
                double threshold = scrollViewer.ScrollableHeight * 0.2;
                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - threshold)
                {
                    LoadMoreCommunityPosts();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            VideoObjectAgain video = button != null ? button.DataContext as VideoObjectAgain : null;

            if (video != null)
            {
                SaveHistoryVideoObject(video);
                Frame.Navigate(typeof(VideoPage), video.videoId);
            }

        }

        private void LoadMoreComments_Click(object sender, RoutedEventArgs e)
        {
          
        }
    }

    public class VideoObjectAgain
    {
        public string videoId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string description { get; set; }
        public string PublishedText { get; set; }
        public string ViewCountText { get; set; }
        public List<ImageObject> videoThumbnails { get; set; }
        public int LengthSeconds { get; set; }
        public string continuation { get; set; }

        public string ThumbnailUrl
        {
            get
            {
                if (videoThumbnails != null && videoThumbnails.Count > 0)
                {
                    return videoThumbnails[0].Url;
                }
                return string.Empty;
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



    public class Banner
    {
        public string Url { get; set; }
    }

    public class CommunityResponse
    {
        public string authorId { get; set; }
        public List<ChannelComment> comments { get; set; }
        public string continuation { get; set; }
    }

    public class ChannelComment
    {
        public string attachmentType { get; set; }


        [JsonIgnore] 
        public AttachmentBase attachment { get; set; }

        public string author { get; set; }
        public bool authorIsChannelOwner { get; set; }
        public string authorId { get; set; }
        public List<ImageObject> authorThumbnails { get; set; }
        public string authorUrl { get; set; }
        public string commentId { get; set; }
        public string content { get; set; }
        public string contentHtml { get; set; }
        public bool isEdited { get; set; }
        public int likeCount { get; set; }
        public int published { get; set; }
        public string publishedText { get; set; }
        public int replyCount { get; set; }
    }



    public abstract class AttachmentBase
    {
        public string type { get; set; }
    }

    public class ImageAttachment : AttachmentBase
    {
        public List<ImageObject> imageThumbnails { get; set; }
    }

    public class MultiImageAttachment : AttachmentBase
    {
        public List<List<ImageObject>> images { get; set; }
    }

    public class PollChoice
    {
        public string text { get; set; }
        public List<ImageObject> image { get; set; }
    }

    public class PollAttachment : AttachmentBase
    {
        public int totalVotes { get; set; }
        public List<PollChoice> choices { get; set; }
    }

    public class PlaylistAttachment : AttachmentBase
    {
     
    }

    public class VideoAttachment : AttachmentBase
    {
      
    }


    public class ChannelObject
    {
        public string author { get; set; }
        public string description { get; set; }
        public List<Banner> authorBanners { get; set; }
        public int subCount { get; set; }
        public int totalViews { get; set; }
    }


    public class ImageObject
    {
        public string Url { get; set; }
    }

    public class VideoList
    {
        public List<VideoObjectAgain> latestVideos { get; set; }
    }

    public class VideoResponse
    {
        public List<VideoObjectAgain> videos { get; set; }
        public string continuation { get; set; }
    }


}
