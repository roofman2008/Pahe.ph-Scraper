using System;
using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class MovieSummery
    {
        public MovieSummery()
        {
            Tags = new List<string>();
        }

        public string Title { get; set; }
        public DateTime Date { get; set; }
        public List<string> Tags { get; set; }
        public int CommentsNo { get; set; }
        public int ViewsNo { get; set; }
        public string CompleteInfoUrl { get; set; }
        public MovieDetails MovieDetails { get; set; }
    }
}