using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class MovieMetadata
    {
        // ReSharper disable once InconsistentNaming
        public string IMDBName { get; set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBSourceUrl { get; set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBImageUrl { get; set; }
        // ReSharper disable once InconsistentNaming
        public float IMDBScore { get; set; }
        // ReSharper disable once InconsistentNaming
        public int IMDBScoreUsersCount { get; set; }
        // ReSharper disable once InconsistentNaming
        public string IMDBDescription { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<string> IMDBDirectors { get; set; }
        // ReSharper disable once InconsistentNaming
        public List<string> IMDBActors { get; set; }
    }
}