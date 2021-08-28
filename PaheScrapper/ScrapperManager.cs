using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using PaheScrapper.Helpers;
using PaheScrapper.Models;
using PaheScrapper.Properties;

namespace PaheScrapper
{
    public class ScrapperManager
    {
        private ScrapperState _scrapperState;
        private int _currentPage;
        private int _maxPage;
        private readonly WebsiteContext _websiteContext;
        private WebRequestHeader _webRequestHeader;

        public ScrapperManager()
        {
            _currentPage = 1;
            _maxPage = _currentPage;
            _scrapperState = ScrapperState.Initiate;
            _websiteContext = new WebsiteContext();
        }

        public WebsiteContext Context => _websiteContext;
        public ScrapperState State => _scrapperState;

        public void ResetState()
        {
            _currentPage = 1;
            _maxPage = _currentPage;
            _scrapperState = ScrapperState.Initiate;
        }

        public void Scrape(Action<ScrapperState> saveState, Action<ScrapperState> emergencySaveState)
        {
            HtmlDocument htmlDocument = null;

            ConsoleHelper.LogBranch("Bypass Surcuri");
            ScrapperWeb.InitializeActiveScrape(1);
            _webRequestHeader = ScrapperWeb.ActiveScrape(0, ScrapperConstants.WebsiteLanding(), ScrapperMethods.BypassSurcuri);
            ScrapperWeb.ReleaseActiveScrape();
            ConsoleHelper.LogInfo("Surcuri Bypassed");

            if (_scrapperState == ScrapperState.Initiate)
            {
                int retryCount = 0;
                int retryLimit = Configuration.Default.HtmlRetryLimit;

                retry:
                try
                {
                    htmlDocument = ScrapperWeb.GetDownloadHtml(ScrapperConstants.WebsiteLanding(), _webRequestHeader);
                    _websiteContext.PagesNo = _maxPage = ScrapperMethods.PagesCount(htmlDocument);
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("Cannot Get Response."))
                    {
                        ConsoleHelper.LogError(e.Message);
                        Console.ReadLine();
                        return;
                    }
                    else
                    {
                        ConsoleHelper.LogError(e.Message);

                        if (NetworkHelper.IsNetworkStable())
                        {
                            ConsoleHelper.LogError($"Retry {retryCount + 1}");
                            if (retryCount < retryLimit - 1)
                            {
                                retryCount++;
                                goto retry;
                            }
                            else
                            {
                                ConsoleHelper.LogCritical("Exceed Retry Limit");
                                Console.ReadLine();
                                return;
                            }
                        }
                        else
                        {
                            if (retryLimit + 1 < Configuration.Default.HtmlRetryMaxLimit)
                            {
                                ConsoleHelper.LogError($"Increase Retry Limit {retryLimit + 1}");
                                retryCount++;
                                retryLimit++;

                                ConsoleHelper.LogInfo($"Wait Stable Connection: [{ScrapperConstants.WebsiteLanding()}]");
                                NetworkHelper.WaitStableNetwork();
                                ConsoleHelper.LogInfo($"Return Stable Connection: [{ScrapperConstants.WebsiteLanding()}]");

                                goto retry;
                            }
                        }
                    }
                }

                _scrapperState = ScrapperState.Summery;
                _currentPage = 1;
                ConsoleHelper.LogInfo($"Pages: {_maxPage}");
                saveState(_scrapperState);
            }

            if (_scrapperState == ScrapperState.Summery)
            {
                if (_currentPage == 1)
                {
                    ConsoleHelper.LogInfo($"Page: {_currentPage}/{_maxPage}");

                    var movieList = ScrapperMethods.MoviesList(htmlDocument);
                    var newMoviesList = movieList.Where(l =>
                        _websiteContext.MovieSummeries.All(c => c.CompleteInfoUrl != l.CompleteInfoUrl)).ToList();

                    _websiteContext.MovieSummeries.AddRange(newMoviesList);
                    ++_currentPage;

                    if (_currentPage % Configuration.Default.HTMLSaveStateThershold == 0)
                        saveState(_scrapperState);

                    if (newMoviesList.Count == 0)
                        goto summeryFinish;
                }

                goto summeryFinish;

                int currentPageSnapshot = _currentPage;

                for (int i = currentPageSnapshot; i < _maxPage; i++)
                {
                    ConsoleHelper.LogInfo($"Page: {_currentPage}/{_maxPage}");

                    _currentPage = i;
                    htmlDocument = ScrapperWeb.GetDownloadHtml(ScrapperConstants.WebsiteLandingPaging(i), _webRequestHeader);

                    var movieList = ScrapperMethods.MoviesList(htmlDocument);
                    var newMoviesList = movieList.Where(l =>
                        _websiteContext.MovieSummeries.All(c => c.CompleteInfoUrl != l.CompleteInfoUrl)).ToList();

                    _websiteContext.MovieSummeries.AddRange(newMoviesList);

                    if (_currentPage % Configuration.Default.HTMLSaveStateThershold == 0)
                        saveState(_scrapperState);

                    if (newMoviesList.Count == 0)
                        goto summeryFinish;
                }

                summeryFinish:
                _scrapperState = ScrapperState.Details;
                _currentPage = 0;
                _maxPage = _websiteContext.MovieSummeries.Count;
                saveState(_scrapperState);
            }
 
            if (_scrapperState == ScrapperState.Details)
            {
                int currentPageSnapshot = _currentPage;

                for (int i = currentPageSnapshot; i < _websiteContext.MovieSummeries.Count; i++)
                {
                    int retryCount = 0;
                    int retryLimit = Configuration.Default.HtmlRetryLimit;
                    ConsoleHelper.LogInfo($"Page: {_currentPage}/{_maxPage}");

                    var movie = _websiteContext.MovieSummeries[i];

                    retry:
                    try
                    {
                        htmlDocument = ScrapperWeb.GetDownloadHtml(movie.CompleteInfoUrl, _webRequestHeader);
                        var vmMovieLookup = ScrapperMethods.DecodeDetailsVM(htmlDocument);
                        var tmpDetails = ScrapperMethods.MovieDetails(htmlDocument, vmMovieLookup);

                        if (movie.MovieDetails == null)
                        {
                            ConsoleHelper.LogBranch($"New Movie [{i}] - Add New Details");

                            ScrapperFixes.NextSiblinMergeDownloadQuality(tmpDetails);
                            movie.MovieDetails = tmpDetails;
                        }
                        else
                        {
                            ConsoleHelper.LogBranch($"Existing Movie [{i}] - Update Current Details");

                            #region Exist Movie Details

                            ScrapperFixes.DeleteEmptyEpisodes(movie.MovieDetails);
                            ScrapperFixes.RemoveDuplicatedEpisodes(movie.MovieDetails);
                            ScrapperFixes.NextSiblinMergeDownloadQuality(movie.MovieDetails);
                            ScrapperFixes.NextSiblinMergeDownloadQuality(tmpDetails);
                            ScrapperFixes.RemoveDuplicatedLink(movie.MovieDetails);

                            movie.MovieDetails.Screenshot = tmpDetails.Screenshot;
                            movie.MovieDetails.IMDBActors = tmpDetails.IMDBActors;
                            movie.MovieDetails.IMDBDescription = tmpDetails.IMDBDescription;
                            movie.MovieDetails.IMDBDirectors = tmpDetails.IMDBDirectors;
                            movie.MovieDetails.IMDBScore = tmpDetails.IMDBScore;
                            movie.MovieDetails.IMDBScoreUsersCount = tmpDetails.IMDBScoreUsersCount;
                            movie.MovieDetails.Chatper = tmpDetails.Chatper;
                            movie.MovieDetails.FileType = tmpDetails.FileType;
                            movie.MovieDetails.IMDBImageUrl = tmpDetails.IMDBImageUrl;
                            movie.MovieDetails.IMDBName = tmpDetails.IMDBName;
                            movie.MovieDetails.IMDBSourceUrl = tmpDetails.IMDBSourceUrl;
                            movie.MovieDetails.Runtime = tmpDetails.Runtime;
                            movie.MovieDetails.SubsceneLink = tmpDetails.SubsceneLink;
                            movie.MovieDetails.Subtitles = tmpDetails.Subtitles;
                            movie.MovieDetails.Trailer = tmpDetails.Trailer;
                            
                            /*Remove Not Found Episodes*/
                            var removedEpisodes = movie.MovieDetails.Episodes.Where(s =>
                                tmpDetails.Episodes.All(l => l.Title != s.Title)).ToList();
                            removedEpisodes.ForEach((rm) => movie.MovieDetails.Episodes.Remove(rm));

                            foreach (var tmpEpisode in tmpDetails.Episodes)
                            {
                                var existEpisode =
                                    movie.MovieDetails.Episodes.SingleOrDefault(l => l.Title == tmpEpisode.Title);

                                if (existEpisode == null)
                                {
                                    movie.MovieDetails.Episodes.Add(tmpEpisode);
                                }
                                else
                                {
                                    /*Remove Not Found DownloadQualities*/
                                    var removedDownloadQualities = existEpisode.DownloadQualities.Where(s =>
                                        tmpEpisode.DownloadQualities.All(l => l.Quality != s.Quality)).ToList();
                                    removedDownloadQualities.ForEach((rdq) => existEpisode.DownloadQualities.Remove(rdq));

                                    foreach (var tmpDownloadQuality in tmpEpisode.DownloadQualities)
                                    {
                                        var existDownloadQuality =
                                            existEpisode.DownloadQualities.SingleOrDefault(l =>
                                                l.Quality == tmpDownloadQuality.Quality);

                                        if (existDownloadQuality == null)
                                        {
                                            existEpisode.DownloadQualities.Add(tmpDownloadQuality);
                                        }
                                        else
                                        {
                                            /*Remove Not Found Links*/
                                            var removedLinks = existDownloadQuality.Links.Where(s =>
                                                tmpDownloadQuality.Links.All(l => l.Url != s.Url)).ToList();
                                            removedLinks.ForEach((rl) => existDownloadQuality.Links.Remove(rl));

                                            foreach (var tmpLink in tmpDownloadQuality.Links)
                                            {
                                                var existLink =
                                                    existDownloadQuality.Links.SingleOrDefault(l =>
                                                        l.Title == tmpLink.Title);

                                                if (existLink == null)
                                                {
                                                    existDownloadQuality.Links.Add(tmpLink);
                                                }
                                                else
                                                {
                                                    if (existLink.Url != tmpLink.Url)
                                                    {
                                                        existLink.Url = tmpLink.Url;
                                                        existLink.ProxiedUrl = tmpLink.ProxiedUrl;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                    catch (Exception e)
                    {
                        if (!e.Message.Contains("Cannot Get Response."))
                        {
                            ConsoleHelper.LogError(e.Message);
                            //Debugger.Break();
                            movie.MovieDetails = null;
                        }
                        else
                        {
                            ConsoleHelper.LogError(e.Message);

                            if (NetworkHelper.IsNetworkStable())
                            {
                                ConsoleHelper.LogError($"Retry {retryCount + 1}: Page {i + 1}");
                                if (retryCount < retryLimit - 1)
                                {
                                    retryCount++;
                                    goto retry;
                                }
                            }
                            else
                            {
                                if (retryLimit + 1 < Configuration.Default.HtmlRetryMaxLimit)
                                {
                                    ConsoleHelper.LogError($"Increase Retry Limit {retryLimit + 1} : Page {i + 1}");
                                    retryCount++;
                                    retryLimit++;

                                    ConsoleHelper.LogInfo($"Wait Stable Connection: [{movie.CompleteInfoUrl}]");
                                    NetworkHelper.WaitStableNetwork();
                                    ConsoleHelper.LogInfo($"Return Stable Connection: [{movie.CompleteInfoUrl}]");

                                    goto retry;
                                }
                            }
                        }
                    }

                    if (_currentPage % Configuration.Default.HTMLSaveStateThershold == 0)
                        saveState(_scrapperState);

                    ++_currentPage;
                }

                _scrapperState = ScrapperState.Sora;
                _currentPage = 0;
                _maxPage = _websiteContext.MovieSummeries.Count;
                saveState(_scrapperState);
            }

            if (_scrapperState == ScrapperState.Sora)
            {
                int scrapperInstance = Configuration.Default.WebDriveInstances;

                ScrapperWeb.InitializeActiveScrape(scrapperInstance);

                int currentPageSnapshot = _currentPage;

                ConsoleHelper.LogStats($@"Count of Proxified Urls: {_websiteContext.MovieSummeries
                    .Where(l => l.MovieDetails != null).SelectMany(l => l.MovieDetails.Episodes
                        .SelectMany(ll => ll.DownloadQualities.SelectMany(lll => lll.Links))).Count(l => l.ProxiedUrl != null)}");
                ConsoleHelper.LogStats($@"Count of Pending Cloaked Urls: {_websiteContext.MovieSummeries
                    .Where(l => l.MovieDetails != null).SelectMany(l => l.MovieDetails.Episodes
                        .SelectMany(ll => ll.DownloadQualities.SelectMany(lll => lll.Links))).Count(l => l.ProxiedUrl == null)}");

                for (int i = currentPageSnapshot; i < _websiteContext.MovieSummeries.Count; i++)
                {
                    ConsoleHelper.LogInfo($"Page: {_currentPage}/{_maxPage}");
                    DateTime preTimestamp = DateTime.Now;
                    
                    var movie = _websiteContext.MovieSummeries[i];
                    List<Link> failedUrls = new List<Link>();

                    if (movie.MovieDetails != null)
                    {
                        retry_phase:
                        var movieLinks = movie.MovieDetails.Episodes
                            .SelectMany(l => l.DownloadQualities.SelectMany(s => s.Links))
                            .Where(l => String.IsNullOrEmpty(l.ProxiedUrl))
                            .Shuffle()
                            .OrderBy(l => failedUrls.All(f => f.Url != l.Url))
                            .ToList();
                        int pending = movieLinks.Count;
                        int nonPending = movie.MovieDetails.Episodes
                            .SelectMany(l => l.DownloadQualities.SelectMany(s => s.Links))
                            .Count(l => !String.IsNullOrEmpty(l.ProxiedUrl));

                        ConsoleHelper.LogStats($"Pending Clocked: {pending}");
                        ConsoleHelper.LogStats($"Available Proxified: {nonPending}");

                        Queue<int> flagQueue = new Queue<int>();

                        for (int f = 0; f < scrapperInstance; f++)
                            flagQueue.Enqueue(f);

                        try
                        {
                            Parallel.ForEach(movieLinks, new ParallelOptions() { MaxDegreeOfParallelism = scrapperInstance }, (movieLink) =>
                            {
                                int retryLimit = Configuration.Default.WebDriveRetryLimit;
                                int retry = 0;
                                int taskId = -1;

                                lock (flagQueue)
                                {
                                    taskId = flagQueue.Dequeue();
                                }

                                ConsoleHelper.LogInfo($"CloakedUrl: {movieLink.Url.GetHost()}");

                                if (Uri.IsWellFormedUriString(movieLink.Url, UriKind.Absolute))
                                {
                                    retryLabel:
                                    try
                                    {
                                        movieLink.ProxiedUrl =
                                            ScrapperWeb.ActiveScrape(taskId, movieLink.Url,
                                                ScrapperMethods.MoviesTrueLinks);

                                        if (failedUrls.Contains(movieLink))
                                            failedUrls.Remove(movieLink);

                                        ConsoleHelper.LogInfo($"ProxiedUrl: {movieLink.ProxiedUrl.GetHost()}");
                                    }
                                    catch (Exception ex)
                                    {
                                        if (retry < retryLimit)
                                        {
                                            ++retry;

                                            if (ex.Message == "Website is down") /*Sit Connection is Down [Error I Defined]*/
                                            {
                                                if (retryLimit + 1 < Configuration.Default.WebDriveRetryMaxLimit)
                                                {
                                                    ++retryLimit;
                                                    ConsoleHelper.LogError($"Increase Retry Limit: {retryLimit}");
                                                }
                                            }
                                            else if (ex.Message.Contains("url timed out after 60 seconds")) /*Error From Webdrive Itself*/
                                            {
                                                if (retryLimit + 1 < Configuration.Default.WebDriveRetryMaxLimit)
                                                {
                                                    ++retryLimit;
                                                    ConsoleHelper.LogError($"Increase Retry Limit: {retryLimit}");
                                                }
                                            }
                                            else if (!NetworkHelper.IsNetworkStable())
                                            {
                                                if (retryLimit + 1 < Configuration.Default.WebDriveRetryMaxLimit)
                                                {
                                                    ++retryLimit;
                                                    ConsoleHelper.LogError($"Increase Retry Limit: {retryLimit}");

                                                    ConsoleHelper.LogInfo($"Wait Stable Connection: [{movieLink.Url.GetHost()}]");
                                                    NetworkHelper.WaitStableNetwork();
                                                    ConsoleHelper.LogInfo($"Return Stable Connection: [{movieLink.Url.GetHost()}]");
                                                }
                                            }

                                            ConsoleHelper.LogCritical($"Retry-{retry} | CloakedUrl: {movieLink.Url.GetHost()} | Reason: {ex.Message}");
                                            goto retryLabel;
                                        }

                                        if (Configuration.Default.WebDriveRestartOnError)
                                        {
                                            if (!failedUrls.Contains(movieLink))
                                                failedUrls.Add(movieLink);

                                            throw;
                                        }
                                        else
                                        {
                                            ConsoleHelper.LogBranch($"Skip CloakedUrl: {movieLink.Url.GetHost()}");
                                        }
                                    }
                                }

                                lock (flagQueue)
                                {
                                    flagQueue.Enqueue(taskId);
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            ConsoleHelper.LogInfo($"Fail: {failedUrls.Count}| Pending: {pending}");

                            if (failedUrls.Count < pending)
                            {
                                emergencySaveState(_scrapperState);
                                goto retry_phase;
                            }
                            else
                            {
                                ConsoleHelper.LogBranch($"Skip Movie: {movie.Title}");
                            }
                        }
                    }
                    else
                    {
                        ConsoleHelper.LogBranch($"Page: {_currentPage} Has No Details");
                    }

                    ConsoleHelper.LogStats($"Time: {DateTime.Now.Subtract(preTimestamp)}");

                    ++_currentPage;

                    if (_currentPage % Configuration.Default.WebDriveSaveStateThershold == 0)
                        saveState(_scrapperState);
                }

                ScrapperWeb.ReleaseActiveScrape();

                _scrapperState = ScrapperState.Complete;
            }
        }

        public string Serialize()
        {
            var settings = new JsonSerializerSettings() { ContractResolver = new MyContractResolver() };
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
            return json;
        }

        public static ScrapperManager Deserialize(string json)
        {
            var settings = new JsonSerializerSettings() { ContractResolver = new MyContractResolver() };
            var manager = JsonConvert.DeserializeObject<ScrapperManager>(json, settings);
            return manager;
        }
    }
}