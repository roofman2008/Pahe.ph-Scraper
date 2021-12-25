using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class DownloadQuality
    {
        public DownloadQuality()
        {
            Links = new List<Link>();
        }

        public DownloadQualityMode Mode { get; set; }
        public string Quality { get; set; }
        public DownloadQualitySize Size { get; set; }
        public string Notes { get; set; }
        public bool HasError { get; set; }

        public List<Link> Links { get; set; }
    }
}