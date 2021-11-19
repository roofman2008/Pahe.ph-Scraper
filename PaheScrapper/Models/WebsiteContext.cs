using System.Collections.Generic;
using Newtonsoft.Json;
using PaheScrapper.Helpers;

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

        public string Serialize()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            return json;
        }

        public static WebsiteContext Deserialize(string json)
        {
            var context = JsonConvert.DeserializeObject<WebsiteContext>(json);
            return context;
        }
    }
}