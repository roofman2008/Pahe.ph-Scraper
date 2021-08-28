using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class MovieEpisode
    {
        public MovieEpisode()
        {
            DownloadQualities = new List<DownloadQuality>();
        }

        public string Title { get; set; }
        public List<DownloadQuality> DownloadQualities { get; set; }

    }
}