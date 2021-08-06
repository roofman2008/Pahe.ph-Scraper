using System;
using Newtonsoft.Json;

namespace WordPressPoster.Models
{
    public class OpenRefineItem
    {
        [JsonProperty("Date")]
        public DateTime Date { get; set; }
        [JsonProperty("Title")]
        public string Title { get; set; }
        [JsonProperty("ViewsNo")]
        public int ViewCount { get; set; }
        [JsonProperty("CommentsNo")]
        public int CommentCount { get; set; }
        [JsonProperty("Tags")]
        public string Tags { get; set; }
        [JsonProperty("CompleteInfoUrl")]
        public string SourceUrl { get; set; }
        [JsonProperty("MovieDetails - Runtime")]
        public string Runtime { get; set; }
        [JsonProperty("MovieDetails - Chatper")]
        public string Chapter { get; set; }
        [JsonProperty("MovieDetails - FileType")]
        public string FileType { get; set; }
        [JsonProperty("MovieDetails - Subtitles")]
        public string Subtitles { get; set; }
        [JsonProperty("MovieDetails - Trailer")]
        public string Trailer { get; set; }
        [JsonProperty("MovieDetails - Screenshot")]
        public string Screenshot { get; set; }
        [JsonProperty("MovieDetails - Subscene")]
        public string Subscense { get; set; }
        [JsonProperty("MovieDetails - IMDBLink - Title")]
        public string IMDBName { get; set; }
        [JsonProperty("MovieDetails - IMDBLink - Url")]
        public string IMDBUrl { get; set; }
        [JsonProperty("MovieDetails - Episodes - Title")]
        public string Tab { get; set; }
        [JsonProperty("MovieDetails - Episodes - DownloadQualities - Quality")]
        public string Section { get; set; }
        [JsonProperty("MovieDetails - Episodes - DownloadQualities - Links - Title")]
        public string DownloadTitle { get; set; }
        [JsonProperty("MovieDetails - Episodes - DownloadQualities - Links - ProxiedUrl")]
        public string DownloadUrl { get; set; }
    }

    public class OpenRefineCollection
    {
        [JsonProperty("rows")]
        public OpenRefineItem[] Items { get; set; }
    }
}