using System.Collections;
using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class DownloadQuality
    {
        public DownloadQuality()
        {
            Links = new List<Link>();
        }

        public string Quality { get; set; }
        public List<Link> Links { get; set; }
    }
}