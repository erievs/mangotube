using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.Phone.UI.Input;

namespace ValleyTube
{
    public sealed partial class SearchPage : Page
    {
        private bool _isSearching = false;
        private bool _hasMoreResults = true;
        private int _currentPage = 1;
        private ObservableCollection<VideoResult> _searchResults = new ObservableCollection<VideoResult>();
        private CancellationTokenSource _cancellationTokenSource;
        private const int ChannelsToLoad = 10; 
private int _currentChannelIndex = 0; 

        public SearchPage()
        {
            this.InitializeComponent();
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
                return;

            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
                e.Handled = true;
            }
            else
            {

                e.Handled = false;
            }
        }

        private async Task Search(string query, int page = 1)
        {
            if (_isSearching || !_hasMoreResults) return;

            _isSearching = true;
            string apiUrl = string.Format(Settings.InvidiousInstance + "/api/v1/search?q={0}&page={1}&sort=relevance",
                                            Uri.EscapeDataString(query), page);

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var searchResults = JsonConvert.DeserializeObject<List<VideoResult>>(response);

                    if (searchResults == null || searchResults.Count == 0)
                    {
                        _hasMoreResults = false;
                        return;
                    }

                    foreach (var video in searchResults)
                    {
                        if (!string.IsNullOrEmpty(video.VideoId))
                        {
                            _searchResults.Add(video);
                        }

                        if (video.Type == "channel")
                        {
                            Debug.WriteLine("Channel found: " + video.AuthorId);
                            _searchResults.Add(video);
                        }
                    }

                    // Only set ItemsSource for the first page
                    if (_currentPage == 1)
                    {
                        SearchListView.ItemsSource = _searchResults;
                    }

                    _currentPage = page;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error fetching search results: " + ex.Message);
                }
                finally
                {
                    _isSearching = false;
                }
            }
        }

        private void SearchListView_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(SearchListView);
            if (scrollViewer != null)
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            }
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null)
            {
                if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 100 && !_isSearching && _hasMoreResults)
                {
                    string query = SearchBox.Text.Trim();
                    if (!string.IsNullOrEmpty(query))
                    {
                        _currentPage++;
                        Debug.WriteLine($"Loading Page: {_currentPage}");
                        await Search(query, _currentPage);
                    }
                }
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
                    if (!string.IsNullOrEmpty(video.AuthorId) && video.Type == "channel")
                    {
                        System.Diagnostics.Debug.WriteLine("Navigating to channel with Author ID: " + video.AuthorId);
                        Frame.Navigate(typeof(ChannelPage), video.AuthorId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Navigating to video with ID: " + video.VideoId);
                        SaveHistory(video);
                        Frame.Navigate(typeof(VideoPage), video.VideoId);
                    }
                }
            }
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

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            var searchBox = sender as TextBox;
            var query = searchBox.Text;

            if (!string.IsNullOrWhiteSpace(query))
            {
                _currentPage = 1;
                _hasMoreResults = true;
                _searchResults.Clear();
                SearchListView.ItemsSource = null;

                try
                {
                    await Task.Delay(300, _cancellationTokenSource.Token);
                    await Search(query);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Search task was canceled.");
                }
            }
            else
            {
                _searchResults.Clear();
                SearchListView.ItemsSource = null;
                _hasMoreResults = true;
            }
        }

        private void SaveHistory(VideoResult video)
        {

        }

        private void SearchListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoResult selectedVideo = SearchListView.SelectedItem as VideoResult;

            if (selectedVideo != null)
            {
                Frame.Navigate(typeof(VideoPage), selectedVideo.VideoId);
            }

            SearchListView.SelectedItem = null;
        }
    }
}
