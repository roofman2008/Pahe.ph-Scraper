using System.Collections.Generic;
using System.Linq;
using PaheScrapper.Models;

namespace PaheScrapper
{
    public static class ScrapperFixes
    {
        public static void DeleteEmptyEpisodes(MovieDetails movieDetails)
        {
            /*Clear Corrupted Episodes from Physical Data*/
            movieDetails.Episodes.Where(l => l.DownloadQualities.Count == 0).ToList().ForEach((e) =>
            {
                movieDetails.Episodes.Remove(e);
            });
        }
        public static void GroupDownloadQuality(MovieDetails movieDetails)
        {
            /*Clear Corrupted DownloadQualities from Physical Data*/
            foreach (var episode in movieDetails.Episodes)
            {
                var duplicatedGroups = episode.DownloadQualities.GroupBy(l => l.Quality).Where(l => l.Count() > 1);

                foreach (var duplicatedGroup in duplicatedGroups)
                {
                    var duplicatedList = episode.DownloadQualities.Where(l => l.Quality == duplicatedGroup.Key).ToList();
                    var aggregate = duplicatedList.Aggregate((f, l) =>
                    {
                        var result = new DownloadQuality()
                        {
                            Quality = f.Quality,
                            Links = new List<Link>()
                        };

                        result.Links.AddRange(f.Links);
                        result.Links.AddRange(l.Links);

                        return result;
                    });

                    duplicatedList.ForEach((dl) => { episode.DownloadQualities.Remove(dl); });
                    episode.DownloadQualities.Add(aggregate);
                }
            }
        }
        public static void RemoveDuplicatedEpisodes(MovieDetails movieDetails)
        {
            var duplicatedGroups = movieDetails.Episodes.GroupBy(l => l.Title).Where(l => l.Count() > 1);

            foreach (var duplicatedGroup in duplicatedGroups)
            {
                var duplicatedList = movieDetails.Episodes.Where(l => l.Title == duplicatedGroup.Key).ToList();
                duplicatedList = duplicatedList.Take(duplicatedList.Count - 1).ToList();
                duplicatedList.ForEach((dl) => { movieDetails.Episodes.Remove(dl); });
            }
        }
        public static void RemoveDuplicatedDownloadQuality(MovieDetails movieDetails)
        {
            /*Clear Corrupted DownloadQualities from Physical Data*/
            foreach (var episode in movieDetails.Episodes)
            {
                var duplicatedGroups = episode.DownloadQualities.GroupBy(l => l.Quality).Where(l => l.Count() > 1);

                foreach (var duplicatedGroup in duplicatedGroups)
                {
                    var duplicatedList = episode.DownloadQualities.Where(l => l.Quality == duplicatedGroup.Key).ToList();
                    duplicatedList = duplicatedList.Take(duplicatedList.Count - 1).ToList();
                    duplicatedList.ForEach((dl) => { episode.DownloadQualities.Remove(dl); });
                }
            }
        }
        public static void RemoveDuplicatedLink(MovieDetails movieDetails)
        {
            /*Clear Corrupted DownloadQualities from Physical Data*/
            foreach (var episode in movieDetails.Episodes)
            {
                foreach (var quality in episode.DownloadQualities)
                {
                    var duplicatedGroups = quality.Links.GroupBy(l => l.Title).Where(l => l.Count() > 1);

                    foreach (var duplicatedGroup in duplicatedGroups)
                    {
                        var duplicatedList = quality.Links.Where(l => l.Title == duplicatedGroup.Key).ToList();
                        duplicatedList = duplicatedList.Take(duplicatedList.Count - 1).ToList();
                        duplicatedList.ForEach((dl) => { quality.Links.Remove(dl); });
                    }
                }
            }
        }
        public static void NextSiblinMergeDownloadQuality(MovieDetails movieDetails)
        {
            foreach (var episode in movieDetails.Episodes)
            {
                List<DownloadQuality> tmpCanidateMerge = new List<DownloadQuality>();
                List<DownloadQuality> tmpResult = new List<DownloadQuality>();

                foreach (var downloadQuality in episode.DownloadQualities)
                {
                    if (!string.IsNullOrEmpty(downloadQuality.Quality))
                    {
                        if (tmpCanidateMerge.Count == 0)
                        {
                            tmpCanidateMerge.Add(downloadQuality);
                        }
                        else if (tmpCanidateMerge.First().Quality != downloadQuality.Quality)
                        {
                            if (tmpCanidateMerge.Count == 1)
                            {
                                tmpResult.Add(tmpCanidateMerge.First());
                                tmpCanidateMerge.Clear();
                                tmpCanidateMerge.Add(downloadQuality);
                            }
                            else if (tmpCanidateMerge.Count > 1)
                            {
                                var aggregate = tmpCanidateMerge.Aggregate((f, l) =>
                                {
                                    var result = new DownloadQuality()
                                    {
                                        Quality = f.Quality,
                                        Links = new List<Link>()
                                    };

                                    result.Links.AddRange(f.Links);
                                    result.Links.AddRange(l.Links);

                                    return result;
                                });
                                tmpResult.Add(aggregate);
                                tmpCanidateMerge.Clear();
                                tmpCanidateMerge.Add(downloadQuality);
                            }
                        }
                    }
                    else if (tmpCanidateMerge.Count > 0 && string.IsNullOrEmpty(downloadQuality.Quality))
                    {
                        tmpCanidateMerge.Add(downloadQuality);
                    }
                    else if (tmpCanidateMerge.Count == 0 && string.IsNullOrEmpty(downloadQuality.Quality))
                    {
                        tmpResult.Add(downloadQuality);
                    }
                }

                if (tmpCanidateMerge.Count > 0)
                {
                    var aggregateLast = tmpCanidateMerge.Aggregate((f, l) =>
                    {
                        var result = new DownloadQuality()
                        {
                            Quality = f.Quality,
                            Links = new List<Link>()
                        };

                        result.Links.AddRange(f.Links);
                        result.Links.AddRange(l.Links);

                        return result;
                    });
                    tmpResult.Add(aggregateLast);
                    tmpCanidateMerge.Clear();
                }

                episode.DownloadQualities = tmpResult;
            }

            GroupDownloadQuality(movieDetails);
        }
    }
}