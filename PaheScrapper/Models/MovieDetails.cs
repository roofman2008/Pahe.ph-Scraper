using System;
using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class MovieDetails
    {
        public MovieDetails()
        {
            Episodes = new List<MovieEpisode>();
            Metadata = new List<MovieMetadata>();
        }

        public MovieDetailsMode MovieDetailsMode { get; set; }
        public List<MovieMetadata> Metadata { get; set; }
        public string FileType { get; set; }
        public TimeSpan Runtime { get; set; }
        public string Subtitles { get; set; }
        public string Chatper { get; set; }
        public Link SubsceneLink { get; set; }
        public Link Screenshot { get; set; }
        public Link Trailer { get; set; }
        public List<MovieEpisode> Episodes { get; set; }
    }
}