using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class WebsiteContext
    {
        public WebsiteContext()
        {
            MovieSummeries = new List<MovieSummery>();
        }

        public int PagesNo { get; set; }
        public List<MovieSummery> MovieSummeries { get; set; }
    }
}