using System.Collections.Generic;

namespace WordPressPoster.Models
{
    public class WordpressPostModel
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string[] Tags { get; set; }
        public string Trailer { get; set; }
        public string Screenshot { get; set; }
        public string Substitles { get; set; }
        public string IMDBName { get; set; }
        public string IMDBLink { get; set; }
        public IEnumerable<WordpressTabModel> Tabs { get; set; }

        public override string ToString()
        {
            return Title ?? "No Title";
        }
    }

    public class WordpressTabModel
    {
        public string Name { get; set; }
        public IEnumerable<WordpressSectionModel> Sections { get; set; }

        public override string ToString()
        {
            return Name ?? "No Tab";
        }
    }

    public class WordpressSectionModel
    {
        public string Name { get; set; }
        public IEnumerable<WordpressDownloadModel> Downloads { get; set; }

        public override string ToString()
        {
            return Name ?? "No Section";
        }
    }

    public class WordpressDownloadModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string SafeId { get; set; }

        public override string ToString()
        {
            return Name ?? "No Download Name";
        }
    }
}