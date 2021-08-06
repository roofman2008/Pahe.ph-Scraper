using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.UI;
using PaheScrapper.Helpers;
using PaheScrapper.Models;
using PaheScrapper.Properties;

namespace PaheScrapper
{
    public static class ScrapperMethods
    {
        public static int ScrapePagesCount(HtmlDocument document)
        {
            var docNode = document.DocumentNode;
            var paginationNode = docNode.Descendants().SingleByNameNClass("div", "pagination");
            var pagesNode = paginationNode.Descendants().SingleByNameNClass("span", "pages");
            var pagesText = pagesNode.InnerText;
            var textSplit = pagesText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var pagesNo = int.Parse(textSplit[textSplit.Length - 1]);
            return pagesNo;
        }

        public static IEnumerable<MovieSummery> ScrapeMoviesList(HtmlDocument document)
        {
            List<MovieSummery> movieSummeries = new List<MovieSummery>();

            var docNode = document.DocumentNode;
            var gridSection = docNode.Descendants().SingleByNameNClass("section", "cat-box recent-box recent-blog");
            var contentNode = gridSection.Descendants().SingleByNameNClass("div", "cat-box-content");
            var movieNodes = contentNode.Descendants().FindByNameNClass("article", "item-list");

            foreach (var movieNode in movieNodes)
            {
                MovieSummery movieSummery = new MovieSummery();

                /*Get Title & Complete Url*/
                var titleNodeContainer = movieNode.Descendants().SingleByNameNClass("h2", "post-box-title");
                var titleNode = titleNodeContainer.Descendants("a").Single();

                if (titleNode.InnerText.ToLower().Contains("Problem With GD Links".ToLower()))
                    continue;

                movieSummery.Title = titleNode.InnerText;
                movieSummery.CompleteInfoUrl = titleNode.Attributes["href"].Value;

                /*Get Metadata (Tags, Date, Counts)*/
                var metadataNode = movieNode.Descendants().SingleByNameNClass("p", "post-meta");
                var timeNode = metadataNode.Descendants().SingleByNameNClass("span", "tie-date");
                var tagsNode = metadataNode.Descendants().SingleByNameNClass("span", "post-cats");
                var commentsNode = metadataNode.Descendants().SingleByNameNClass("span", "post-comments");
                //var viewsNode = metadataNode.Descendants().SingleByNameNClass("span", "post-views");

                movieSummery.Date = DateTime.Parse(timeNode.InnerText);
                //movieSummery.ViewsNo = int.Parse(viewsNode.InnerText.Replace(",", ""));
                movieSummery.CommentsNo = commentsNode.InnerText.Contains("Comments Off")
                    ? 0
                    : int.Parse(commentsNode.InnerText.Replace(",", ""));
                movieSummery.Tags = tagsNode.InnerText.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
                movieSummery.MovieDetails = null;

                movieSummeries.Add(movieSummery);
            }

            return movieSummeries;
        }

        public static MovieDetails ScrapeMovieDetails(HtmlDocument document)
        {
            MovieDetails details = new MovieDetails();

            var docNode = document.DocumentNode;
            var imdbNode = docNode.Descendants().SingleOrDefaultByNameNClass("a", "imdbwp__link");
            var imdbRate = docNode.Descendants().SingleOrDefaultByNameNClass("span", "imdbwp__rating");
            var imdbDescription = docNode.Descendants().SingleOrDefaultByNameNClass("div", "imdbwp__teaser");
            var imdbFooter = docNode.Descendants().SingleOrDefaultByNameNClass("div", "imdbwp__footer");
            
            if (imdbNode != null)
            {
                details.IMDBName = imdbNode.Attributes["title"].Value;
                details.IMDBSourceUrl = imdbNode.Attributes["href"].Value;
                var imdbImage = imdbNode.Descendants().Single(l => l.Name == "img");
                details.IMDBImageUrl = imdbImage.Attributes["src"].Value;
            }

            if (imdbRate != null)
            {
                if (imdbRate.InnerText.ToLower().Contains("rating:"))
                {
                    var content = imdbRate.InnerText.ToLower().Replace("rating:", "").TrimStart().TrimEnd();

                    if (!string.IsNullOrEmpty(content))
                    {
                        string rateText = content.Substring(0, content.IndexOf("/")).TrimEnd();
                        details.IMDBScore = float.Parse(rateText);

                        if (content.Contains("from"))
                        {
                            string usersCountText = content.Substring(content.IndexOf("from") + 4,
                                    content.IndexOf("users") - content.IndexOf("from") - 4)
                                .TrimStart().TrimEnd().Replace(",", "");
                            details.IMDBScoreUsersCount = int.Parse(usersCountText);
                        }
                    }
                }
            }

            if (imdbDescription != null)
            {
                var content = imdbDescription.InnerText;
                details.IMDBDescription = content.ToLower() == "n/a" ? null : content;
            }

            if (imdbFooter != null)
            {
                var directorsNode = imdbFooter.Descendants().FirstOrDefault(l=>l.Name == "strong" && l.InnerText == "Director:")?.NextSibling.NextSibling;
                var actorsNode = imdbFooter.Descendants().FirstOrDefault(l => l.Name == "strong" && l.InnerText == "Actors:")?.NextSibling.NextSibling;
                details.IMDBDirectors =
                    directorsNode?.InnerText.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
                details.IMDBActors =
                    actorsNode?.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(string.IsNullOrEmpty(details.IMDBSourceUrl) ? "#Error# [No IMDB Info]" : details.IMDBSourceUrl);

            var detailsNode = docNode.Descendants().FirstOrDefault(l => l.Name == "code");

            if (detailsNode != null)
            {
                var fileNode = detailsNode.Descendants().SingleOrDefault(l => l.InnerText.Contains("File ."));
                var runtimeNode = detailsNode.Descendants().SingleOrDefault(l => l.InnerText.Contains("Runtime ."));
                var subtitlesNode = detailsNode.Descendants().FirstOrDefault(l => l.InnerText.Contains("Subtitles ."));
                var chapterNode = detailsNode.Descendants().SingleOrDefault(l => l.InnerText.Contains("Chapter ."));
                var subsceneLinkNode = detailsNode.Descendants()
                    .FirstOrDefault(l => l.InnerText.Contains("Subscene Link ."))
                    ?.NextSibling;
                var screenshotNode = detailsNode.Descendants()
                    .SingleOrDefault(l => l.InnerText.Contains("Screenshot ."))
                    ?.NextSibling;
                var trailerNode = detailsNode.Descendants().SingleOrDefault(l => l.InnerText.Contains("Trailer ."))
                    ?.NextSibling;

                details.FileType = fileNode?.InnerText.Split(new char[] {':'})[1].TrimStart().TrimEnd();
                details.Runtime = RuntimeParser.Parse(runtimeNode?.InnerText.Split(new char[] {':'})[1]);
                details.Subtitles = subtitlesNode?.InnerText.Split(new char[] {':'})[1].TrimStart().TrimEnd();
                details.Chatper = chapterNode?.InnerText.Split(new char[] {':'})[1].TrimStart().TrimEnd();
                details.SubsceneLink = subsceneLinkNode != null && subsceneLinkNode.Name == "a"
                    ? new Link()
                    {
                        Title = subsceneLinkNode.InnerText,
                        Url = subsceneLinkNode.Attributes["href"].Value
                    }
                    : null;
                details.Screenshot = screenshotNode != null && screenshotNode.Name == "a"
                    ? new Link()
                    {
                        Title = screenshotNode.InnerText,
                        Url = screenshotNode.Attributes["href"].Value
                    }
                    : null;
                details.Trailer = trailerNode != null && trailerNode.Name == "a"
                    ? new Link()
                    {
                        Title = trailerNode.InnerText,
                        Url = trailerNode.Attributes["href"].Value
                    }
                    : null;
            }

            if (docNode.Descendants().CountByNameNClass("div", "box download  ") == 1)
            {
                var downloadNode = docNode.Descendants().SingleByNameNClass("div", "box download  ");

                var downloadInnerNode = downloadNode.Descendants().SingleByNameNClass("div", "box-inner-block");
                var unwantedNode = downloadInnerNode.Descendants().SingleOrDefaultByNameNClass("i", "fa tie-shortcode-boxicon");
                unwantedNode?.Remove();
                var downloadHtmls = downloadInnerNode.InnerHtml
                    .Replace("<br>", "")
                    .Replace("</p>", "")
                    .Replace("<p><b>", "<p><b><b>")
                    .TrimStart().TrimEnd()
                    .Split(new string[] {"&nbsp;\n", "<p><b>" }, StringSplitOptions.RemoveEmptyEntries);

                downloadHtmls = downloadHtmls.Where(l => !l.Contains("&nbsp;") && l.Contains("<a") && l.Contains("<b>")).ToArray();

                MovieEpisode episode = new MovieEpisode()
                {
                    Title = null
                };

                foreach (var downloadHtml in downloadHtmls)
                {
                    MemoryStream ms = new MemoryStream();
                    TextWriter tw = new StreamWriter(ms);

                    tw.Write(downloadHtml);
                    tw.Flush();
                    ms.Position = 0;

                    TextReader tr = new StreamReader(ms);
                    HtmlDocument tmpDoc = new HtmlDocument();
                    tmpDoc.Load(tr);
                    tr.Close();
                    tw.Close();
                    ms.Close();
                    ms.Dispose();
                    tr.Dispose();
                    tw.Dispose();

                    var qualityNode = tmpDoc.DocumentNode.Descendants().LastOrDefault(l => l.Name == "b");
                    var downloadLinkNodes = tmpDoc.DocumentNode.Descendants().Where(l => l.Name == "a");

                    DownloadQuality downloadQuality = new DownloadQuality()
                    {
                        Quality = qualityNode.InnerText
                    };

                    foreach (var downloadLinkNode in downloadLinkNodes)
                    {
                        downloadQuality.Links.Add(new Link()
                        {
                            Title = downloadLinkNode.InnerText,
                            Url = downloadLinkNode.Attributes["href"].Value
                        });
                    }

                    episode.DownloadQualities.Add(downloadQuality);
                }

                details.Episodes.Add(episode);
            }
            else if (docNode.Descendants().CountByNameNClass("div", "box download  ") >= 1)
            {
                int index = 0;
                var tabsNodes = docNode
                    .Descendants().FindByNameNClass("ul", "tabs-nav")
                    .SelectMany(l=>l.Descendants("li"));
                var paneNodes = docNode.Descendants().FindByNameNClass("div", "pane").Take(tabsNodes.Count());

                foreach (var paneNode in paneNodes)
                {
                    var downloadNodes = paneNode.Descendants().FindByNameNClass("div", "box download  ");

                    foreach (var downloadNode in downloadNodes)
                    {
                        var downloadInnerNode = downloadNode.Descendants().SingleByNameNClass("div", "box-inner-block");
                        var episodeTitle = downloadNode.Descendants().FirstOrDefault(l => l.Name == "span" || l.Name == "b" || l.Name == "strong");
                        var episodeTitleExtra = downloadNode.Descendants().FirstOrDefault(l => l.Name == "span" || l.Name == "b" || l.Name == "strong")?.NextSibling;
                        var unwantedNode = downloadInnerNode.Descendants().SingleOrDefaultByNameNClass("i", "fa tie-shortcode-boxicon");
                        episodeTitle?.Remove();
                        episodeTitleExtra?.Remove();
                        unwantedNode?.Remove();
                        var downloadHtmls = downloadInnerNode.InnerHtml
                            .Replace("<br>", "")
                            .Replace("</p>", "")
                            .Replace("<p><b>", "<p><b><br>")
                            .TrimStart().TrimEnd()
                            .Split(new string[] { "&nbsp;\n", "<p>" }, StringSplitOptions.RemoveEmptyEntries);

                        downloadHtmls = downloadHtmls.Where(l => !l.Contains("&nbsp;") && l.Contains("<a")).ToArray();

                        MovieEpisode episode = new MovieEpisode()
                        {
                            Title = tabsNodes.Skip(index).Take(1).First().InnerText,
                            Notes = episodeTitle?.InnerText.TrimStart().TrimEnd() + episodeTitleExtra?.InnerText.TrimEnd()
                        };

                        foreach (var downloadHtml in downloadHtmls)
                        {
                            MemoryStream ms = new MemoryStream();
                            TextWriter tw = new StreamWriter(ms);

                            tw.Write(downloadHtml);
                            tw.Flush();
                            ms.Position = 0;

                            TextReader tr = new StreamReader(ms);
                            HtmlDocument tmpDoc = new HtmlDocument();
                            tmpDoc.Load(tr);
                            tmpDoc.DocumentNode.Descendants().Where(l => l.Name == "span").ToList()
                                .ForEach(l => l.Remove());
                            tr.Close();
                            tw.Close();
                            ms.Close();
                            ms.Dispose();
                            tr.Dispose();
                            tw.Dispose();

                            var qualityNode = tmpDoc.DocumentNode.Descendants().LastOrDefault(l => l.Name == "b");
                            var downloadLinkNodes = tmpDoc.DocumentNode.Descendants().Where(l => l.Name == "a");

                            DownloadQuality downloadQuality = new DownloadQuality()
                            {
                                Quality = qualityNode?.InnerText
                            };

                            foreach (var downloadLinkNode in downloadLinkNodes)
                            {
                                downloadQuality.Links.Add(new Link()
                                {
                                    Title = downloadLinkNode.InnerText,
                                    Url = downloadLinkNode.Attributes["href"].Value
                                });
                            }

                            episode.DownloadQualities.Add(downloadQuality);
                        }

                        details.Episodes.Add(episode);
                    }


                    index++;
                }
            }

            return details;
        }

        public static string ScrapeMoviesTrueLinks(IWebDriver driver, int currentWindow, string[] windows, Semaphore semaphore)
        {
            int taskTimeout = Configuration.Default.WebDriveTaskTimeout; /*sec*/
            int timeout = 30; /*sec*/
            Stack<string> handles = new Stack<string>();
            DateTime loginTime = DateTime.Now;

            semaphore.WaitOne();
            handles.Push(windows[currentWindow]);
            var url = driver.SwitchTo().Window(handles.Peek()).Url.ToLower();
            semaphore.Release();          

            while (true)
            {
                Thread.Sleep(1000);

                semaphore.WaitOne();

                var pageSource = driver.SwitchTo().Window(handles.Peek()).PageSource.ToLower();
                if (
                    //Case 1
                    (pageSource.Contains("500") && pageSource.Contains("internal") &&
                     pageSource.Contains("server") && pageSource.Contains("error") &&
                     pageSource.Contains("nginx")) ||
                    //Case 2
                    (pageSource.Contains("error") && pageSource.Contains("bad") &&
                     pageSource.Contains("gateway") && (pageSource.Contains("504") ||
                                                        pageSource.Contains("502"))) ||
                    //Case 3
                    (pageSource.Contains("error") && pageSource.Contains("bad") &&
                     pageSource.Contains("connection timed out") && pageSource.Contains("522")) ||
                    //Case 4
                    (pageSource.Contains("503") && pageSource.Contains("service") &&
                     pageSource.Contains("temporarily") && pageSource.Contains("unavailable") &&
                     pageSource.Contains("nginx")) ||
                    //Case 5
                    (pageSource.Contains("site") && pageSource.Contains("technical") &&
                     pageSource.Contains("difficulties") && pageSource.Contains("experiencing") &&
                     pageSource.Contains("the")) ||
                    //Case 6
                    (pageSource.Contains("fatal") && pageSource.Contains("error") &&
                     pageSource.Contains("on line")) ||
                    //Case 6
                    (pageSource.Contains("503") && pageSource.Contains("service") &&
                     pageSource.Contains("temporarily") && pageSource.Contains("unavailable") && pageSource.Contains("tengine"))
                )
                {
                    foreach (var handle in handles)
                        if (handle != windows[currentWindow])
                            driver.SwitchTo().Window(handle).Close();

                    handles.Clear();
                    handles.Push(windows[currentWindow]);

                    throw new Exception("Website is down");
                }

                if (url.Contains("lonelymoon") || url.Contains("intercelestial") || url.Contains("sweetlantern"))
                {
                    if (driver.SwitchTo().Window(handles.Peek()).PageSource.Contains("Redirect to nowhere"))
                    {
                        foreach(var handle in handles)
                            if (handle != windows[currentWindow])
                                driver.SwitchTo().Window(handle).Close();

                        handles.Clear();
                        handles.Push(windows[currentWindow]);

                        semaphore.Release();
                        return "Link Expired";
                    }

                    if (driver.SwitchTo().Window(handles.Peek()).PageSource.Contains("Please verify that you are human"))
                    {
                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("div"), By.ClassName("wait")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                continue;
                            }
                            else
                            {
                                foreach (var handle in handles)
                                    if (handle != windows[currentWindow])
                                        driver.SwitchTo().Window(handle).Close();

                                handles.Clear();
                                handles.Push(windows[currentWindow]);

                                throw new Exception("Website has error");
                            }
                        }

                        var capachaWaiter = new WebDriverWait(driver.SwitchTo().Window(handles.Peek()),
                            TimeSpan.FromSeconds(timeout));

                        var capachaElement = capachaWaiter.Until(
                            ExpectedConditions.ElementIsVisible(new ByAll(By.TagName("div"), By.ClassName("wait"))));
                        if (capachaElement != null)
                        {
                            while (true)
                            {
                                var innerElements = capachaElement.FindElement(By.TagName("img"));
                                var value = innerElements.GetAttribute("src");

                                if (value.Contains("robot.png"))
                                    break;

                                Thread.Sleep(10);
                            }

                            capachaElement.Click();
                            semaphore.Release();
                            continue;
                        }
                        else
                        {
                            foreach (var handle in handles)
                                if (handle != windows[currentWindow])
                                    driver.SwitchTo().Window(handle).Close();

                            handles.Clear();
                            handles.Push(windows[currentWindow]);

                            throw new Exception("Website has error");
                        }
                    }

                    {
                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("a"), By.Id("generater")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                continue;
                            }
                            else
                            {
                                foreach (var handle in handles)
                                    if (handle != windows[currentWindow])
                                        driver.SwitchTo().Window(handle).Close();

                                handles.Clear();
                                handles.Push(windows[currentWindow]);

                                throw new Exception("Website has error");
                            }
                        }

                        var generatorWaiter = new WebDriverWait(driver.SwitchTo().Window(handles.Peek()),
                            TimeSpan.FromSeconds(timeout));

                        var generatorElement = generatorWaiter.Until(
                            ExpectedConditions.ElementIsVisible(new ByAll(By.TagName("a"), By.Id("generater"))));
                        if (generatorElement != null)
                        {
                            while (true)
                            {
                                var innerElements = generatorElement.FindElement(By.TagName("img"));
                                var value = innerElements.GetAttribute("src");

                                if (value.Contains("start.png") || value.Contains("generate-link.png"))
                                    break;

                                Thread.Sleep(10);
                            }

                            generatorElement.Click();
                        }
                        else
                        {
                            foreach (var handle in handles)
                                if (handle != windows[currentWindow])
                                    driver.SwitchTo().Window(handle).Close();

                            handles.Clear();
                            handles.Push(windows[currentWindow]);

                            throw new Exception("Website has error");
                        }
                    }

                    {
                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("img"), By.Id("showlink")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                continue;
                            }
                            else
                            {
                                foreach (var handle in handles)
                                    if (handle != windows[currentWindow])
                                        driver.SwitchTo().Window(handle).Close();

                                handles.Clear();
                                handles.Push(windows[currentWindow]);

                                throw new Exception("Website has error");
                            }
                        }

                        var downloadWaiter = new WebDriverWait(driver.SwitchTo().Window(handles.Peek()),
                            TimeSpan.FromSeconds(timeout));
                        var downloadElement = downloadWaiter.Until(
                            ExpectedConditions.ElementIsVisible(new ByAll(By.TagName("img"), By.Id("showlink"))));

                        while (true)
                        {
                            var value = downloadElement.GetAttribute("src");

                            if (value.Contains("end.png") || value.Contains("download.png"))
                                break;

                            Thread.Sleep(10);
                        }

                        /*Get Current Windows*/
                        var oldHandles = driver.WindowHandles;

                        downloadElement.Click();

                        while (driver.WindowHandles.Count <= oldHandles.Count)
                        { Thread.Sleep(10); }

                        var newHandle = driver.WindowHandles.First(l => oldHandles.All(s => s != l));
                        handles.Push(newHandle);
                    }
                }
                else if (url.Contains("spacetica"))
                {
                    var pageSource_TMP = driver.SwitchTo().Window(handles.Peek()).PageSource.ToLower();
                    if (pageSource_TMP.Contains("404") && pageSource_TMP.Contains("not found"))
                    {
                        foreach (var handle in handles)
                            if (handle != windows[currentWindow])
                                driver.SwitchTo().Window(handle).Close();

                        handles.Clear();
                        handles.Push(windows[currentWindow]);

                        semaphore.Release();
                        return "Link Expired";
                    }

                    var elements = driver.SwitchTo().Window(handles.Peek()).FindElements(new ByAll(By.TagName("a"), By.LinkText("Continue")));

                    if (elements.Count == 0)
                    {
                        /*Need Unlock*/
                        if (driver.SwitchTo().Window(handles.Peek())
                                .FindElements(new ByAll(By.TagName("button"))).Count(l=>l.Text == "Unlock") == 1)
                        {
                            foreach (var handle in handles)
                                if (handle != windows[currentWindow])
                                    driver.SwitchTo().Window(handle).Close();

                            handles.Clear();
                            handles.Push(windows[currentWindow]);

                            semaphore.Release();

                            return "Need Unlock Password";
                        }

                        if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                        {
                            semaphore.Release();
                            Thread.Sleep(10);
                            continue;
                        }
                        else
                        {
                            foreach (var handle in handles)
                                if (handle != windows[currentWindow])
                                    driver.SwitchTo().Window(handle).Close();

                            handles.Clear();
                            handles.Push(windows[currentWindow]);

                            throw new Exception("Website has error");
                        }
                    }

                    var downloadWaiter = new WebDriverWait(driver.SwitchTo().Window(handles.Peek()), TimeSpan.FromSeconds(timeout));
                    var downloadElement = downloadWaiter.Until(ExpectedConditions.ElementIsVisible(new ByAll(By.TagName("a"), By.LinkText("Continue"))));
                    var downloadLink = downloadElement.GetAttribute("href");

                    foreach (var handle in handles)
                        if (handle != windows[currentWindow])
                            driver.SwitchTo().Window(handle).Close();

                    handles.Clear();
                    handles.Push(windows[currentWindow]);

                    semaphore.Release();

                    return downloadLink;
                }
                else if (url == "about:blank" || String.IsNullOrEmpty(url))
                {
                    if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                    {
                        Thread.Sleep(10);
                    }
                    else
                    {
                        foreach (var handle in handles)
                            if (handle != windows[currentWindow])
                                driver.SwitchTo().Window(handle).Close();

                        handles.Clear();
                        handles.Push(windows[currentWindow]);

                        throw new Exception("Website has error");
                    }
                }
                else
                {
                    var otherUrl = driver.SwitchTo().Window(handles.Peek()).Url;

                    foreach (var handle in handles)
                        if (handle != windows[currentWindow])
                            driver.SwitchTo().Window(handle).Close();

                    handles.Clear();
                    handles.Push(windows[currentWindow]);

                    semaphore.Release();

                    return otherUrl;
                }

                url = driver.SwitchTo().Window(handles.Peek()).Url;
                semaphore.Release();
            }
        }
    }
}