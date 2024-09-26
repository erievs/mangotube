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

namespace ValleyTube
{
    public sealed partial class ChannelPage : Page
    {
        private string channelId;

        public ChannelPage()
        {
            this.InitializeComponent();
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
                        ChannelBanner.Source = new BitmapImage(new Uri(bannerUrl));
                        ChannelStatsTextBlock.Text = $"Subscribers: {channelData.subCount}\nTotal Views: {channelData.totalViews}";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading channel details: " + ex.Message);
                }
            }
        }

        private async Task LoadChannelVideos()
        {
            string videosUrl = $"{Settings.InvidiousInstance}/api/v1/channels/{channelId}/latest";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(videosUrl);
                    System.Diagnostics.Debug.WriteLine("Raw JSON Response: " + response);

                    var videoResponse = JsonConvert.DeserializeObject<VideoResponse>(response);

                    if (videoResponse != null && videoResponse.videos != null)
                    {
                        VideosListView.ItemsSource = videoResponse.videos; 
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
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error: communityData is null or comments is null.");
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
                    System.Diagnostics.Debug.WriteLine("Error loading community posts: " + ex.Message);
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
    }


}
