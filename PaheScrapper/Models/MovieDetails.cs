using System;
using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class MovieDetails
    {
        public MovieDetails()
        {
            Episodes = new List<MovieEpisode>();
        }

        // ReSharper disable once InconsistentNaming
        public string IMDBName { get; set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBSourceUrl { get; set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBImageUrl { get; set; }
        // ReSharper disable once InconsistentNaming
        public float IMDBScore { get; set; }
        // ReSharper disable once InconsistentNaming
        public int IMDBScoreUsersCount { get;set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBDescription { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<string> IMDBDirectors { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<string> IMDBActors { get; set; }
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