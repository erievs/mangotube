using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValleyTube
{
    public class TrendingVideo
    {
        public string Title { get; set; }
        public string VideoId { get; set; }
        public List<Thumbnail> VideoThumbnails { get; set; }
        public int LengthSeconds { get; set; }
        public long ViewCount { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
        public string AuthorUrl { get; set; }
        public long Published { get; set; }
        public string PublishedText { get; set; }
        public string Description { get; set; }
        public string DescriptionHtml { get; set; }
        public bool LiveNow { get; set; }
        public bool Paid { get; set; }
        public bool Premium { get; set; }
        public string Id { get; set; }


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

        public string ThumbnailUrl
        {
            get
            {
                if (VideoThumbnails != null && VideoThumbnails.Count > 0)
                {
                    return VideoThumbnails[0].Url;
                }
                return string.Empty;
            }
        }

        public string ViewCountText
        {
            get
            {
                return ViewCount.ToString() + " views";
            }
        }
    }
}
