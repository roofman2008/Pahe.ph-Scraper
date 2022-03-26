﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using PaheScrapper.Helpers;
using PaheScrapper.Models;
using PaheScrapper.Properties;
using SeleniumExtras.PageObjects;
using SeleniumExtras.WaitHelpers;

namespace PaheScrapper
{
    public static class ScrapperMethods
    {
        public static int PagesCount(HtmlDocument document)
        {
            var docNode = document.DocumentNode;
            var paginationNode = docNode.Descendants().SingleByNameNClass("div", "pagination");
            var pagesNode = paginationNode.Descendants().SingleByNameNClass("span", "pages");
            var pagesText = pagesNode.InnerText;
            var textSplit = pagesText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var pagesNo = int.Parse(textSplit[textSplit.Length - 1].Replace(",", ""));
            return pagesNo;
        }

        public static IEnumerable<MovieSummery> MoviesList(HtmlDocument document)
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
                movieSummery.Tags = tagsNode.InnerText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                movieSummery.MovieDetails = null;

                movieSummeries.Add(movieSummery);
            }

            return movieSummeries;
        }

        public static MovieDetails MovieDetails(HtmlDocument document)
        {
            MovieDetails details = new MovieDetails();
            details.MovieDetailsMode = MovieDetailsMode.None;

            var vmMovieLookup = DecodeDetailsVM(document);

            details.MovieDetailsMode |= vmMovieLookup.IsVMAvailable() ? MovieDetailsMode.Obfuscation : MovieDetailsMode.None;

            var docNode = document.DocumentNode;
            var imdbNodes = docNode.Descendants().FindByNameNContainClass("div", "imdbwp");

            if (imdbNodes != null && imdbNodes.Count() > 0)
            {
                foreach (var imdbNode in imdbNodes)
                {
                    MovieMetadata movieMetadata = new MovieMetadata();

                    var imdbLink = imdbNode.Descendants().SingleByNameNClass("a", "imdbwp__link");
                    var imdbRate = imdbNode.Descendants().SingleByNameNClass("span", "imdbwp__rating");
                    var imdbDescription = imdbNode.Descendants().SingleByNameNClass("div", "imdbwp__teaser");
                    var imdbFooter = imdbNode.Descendants().SingleByNameNClass("div", "imdbwp__footer");


                    if (imdbLink != null)
                    {
                        movieMetadata.IMDBName = imdbLink.Attributes["title"].Value;
                        movieMetadata.IMDBSourceUrl = imdbLink.Attributes["href"].Value;
                        var imdbImage = imdbLink.Descendants().Single(l => l.Name == "img");
                        movieMetadata.IMDBImageUrl = imdbImage.Attributes["src"].Value;
                    }

                    if (imdbRate != null)
                    {
                        if (imdbRate.InnerText.ToLower().Contains("rating:"))
                        {
                            var content = imdbRate.InnerText.ToLower().Replace("rating:", "").TrimStart().TrimEnd();

                            if (!string.IsNullOrEmpty(content))
                            {
                                string rateText = content.Substring(0, content.IndexOf("/")).TrimEnd();
                                movieMetadata.IMDBScore = float.Parse(rateText);

                                if (content.Contains("from"))
                                {
                                    string usersCountText = content.Substring(content.IndexOf("from") + 4,
                                            content.IndexOf("users") - content.IndexOf("from") - 4)
                                        .TrimStart().TrimEnd().Replace(",", "");
                                    movieMetadata.IMDBScoreUsersCount = int.Parse(usersCountText);
                                }
                            }
                        }
                    }

                    if (imdbDescription != null)
                    {
                        var content = imdbDescription.InnerText;
                        movieMetadata.IMDBDescription = content.ToLower() == "n/a" ? null : content;
                    }

                    if (imdbFooter != null)
                    {
                        var directorsNode = imdbFooter.Descendants().FirstOrDefault(l => l.Name == "strong" && (l.InnerText == "Directors:" || l.InnerText == "Director:"))?.NextSibling.NextSibling;
                        var actorsNode = imdbFooter.Descendants().FirstOrDefault(l => l.Name == "strong" && (l.InnerText == "Actors:" || l.InnerText=="Actor:"))?.NextSibling.NextSibling;
                        var creatorsNode = imdbFooter.Descendants().FirstOrDefault(l => l.Name == "strong" && (l.InnerText == "Creators:" || l.InnerText == "Creator:"))?.NextSibling.NextSibling;
                        movieMetadata.IMDBDirectors =
                            directorsNode?.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart().TrimEnd()).ToList();
                        movieMetadata.IMDBActors =
                            actorsNode?.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart().TrimEnd()).ToList();
                        movieMetadata.IMDBCreators =
                            creatorsNode?.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimStart().TrimEnd()).ToList();
                    }

                    ConsoleHelper.LogInfo(string.IsNullOrEmpty(movieMetadata.IMDBSourceUrl) ? "#Error# [No IMDB Info]" : movieMetadata.IMDBSourceUrl);

                    details.Metadata.Add(movieMetadata);
                }
            }

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

                details.FileType = fileNode?.InnerText.Split(new char[] { ':' })[1].TrimStart().TrimEnd();
                details.Runtime = RuntimeParser.Parse(runtimeNode?.InnerText.Split(new char[] { ':' })[1]);
                details.Subtitles = subtitlesNode?.InnerText.Split(new char[] { ':' })[1].TrimStart().TrimEnd();
                details.Chatper = chapterNode?.InnerText.Split(new char[] { ':' })[1].TrimStart().TrimEnd();
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

            void ProcessHRefLinks(MovieEpisode tmp_Episode, HtmlNode tmp_downloadNode)
            {
                var downloadHtmls = HtmlAnalysis.LinesToArray(HtmlAnalysis.CleanHtml(tmp_downloadNode.InnerHtml));

                DownloadQuality downloadQuality = new DownloadQuality()
                {
                    Mode = DownloadQualityMode.None
                };

                foreach (var downloadHtml in downloadHtmls)
                {
                    //var note = HtmlAnalysis.ExtractNote(downloadHtml);
                    var size = HtmlAnalysis.ExtractSize(downloadHtml);
                    var quality = HtmlAnalysis.ExtractQuality(downloadHtml);
                    var hRefs = HtmlAnalysis.ExtractHRef(downloadHtml);

                    if ((downloadQuality.Mode == DownloadQualityMode.CompleteNoNotes ||
                         downloadQuality.Mode == DownloadQualityMode.Complete) &&
                        hRefs.Length == 0 &&
                        (/*!string.IsNullOrEmpty(note) ||*/ !string.IsNullOrEmpty(size) || !string.IsNullOrEmpty(quality)))
                    {
                        tmp_Episode.DownloadQualities.Add(downloadQuality);

                        downloadQuality = new DownloadQuality()
                        {
                            Mode = DownloadQualityMode.None
                        };
                    }

                    if (/*!string.IsNullOrEmpty(note) &&*/ !downloadQuality.Mode.HasFlag(DownloadQualityMode.Note))
                    {
                        //downloadQuality.Notes = note;
                        downloadQuality.Mode |= DownloadQualityMode.Note;
                    }

                    if (!string.IsNullOrEmpty(size) && !downloadQuality.Mode.HasFlag(DownloadQualityMode.Size))
                    {
                        //downloadQuality.Size = size;
                        downloadQuality.Mode |= DownloadQualityMode.Size;
                    }

                    if (!string.IsNullOrEmpty(quality) && !downloadQuality.Mode.HasFlag(DownloadQualityMode.Quality))
                    {
                        downloadQuality.Quality = quality;
                        downloadQuality.Mode |= DownloadQualityMode.Quality;
                    }

                    if (hRefs.Length > 0)
                    {
                        foreach (var hRef in hRefs)
                        {
                            downloadQuality.Links.Add(new Link()
                            {
                                Title = hRef.Title,
                                Url = hRef.Url,
                                ProxiedUrl = null
                            });
                        }

                        downloadQuality.Mode |= DownloadQualityMode.Links;
                    }
                }

                if (downloadQuality.Mode == DownloadQualityMode.CompleteNoNotes ||
                    downloadQuality.Mode == DownloadQualityMode.Complete)
                    tmp_Episode.DownloadQualities.Add(downloadQuality);
            }

            void ProcessVMLinks(MovieEpisode tmp_Episode, HtmlNode tmp_downloadNode, string tabName)
            {
                tmp_downloadNode.Descendants().Where(l => l.Name == "a").ToList().ForEach(l => { l?.Remove(); });

                var downloadHtmls = tmp_downloadNode.InnerHtml.Replace("<br>", "")
                    .Replace("</p>", "")
                    .Replace("<p><b>", "<p><b><b>")
                    //.Replace('\n', ' ')
                    .TrimStart()
                    .TrimEnd()
                    .Split(new string[] { "&nbsp;\n", "<p><b>" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => l.TrimStart().TrimEnd() != "&nbsp;" && !string.IsNullOrEmpty(l))
                    .ToArray();

                /*Fix Array Split*/
                List<string> downloadHtmlsList = new List<string>();
                int downloadHtmlsIndex = -1;
                for (int i = 0; i < downloadHtmls.Length; i++)
                {
                    if (i == 0)
                    {
                        downloadHtmlsList.Add(downloadHtmls[i]);
                        downloadHtmlsIndex++;
                    }
                    else
                    {
                        if (downloadHtmls[i].Length >= 4)
                        {
                            var template = downloadHtmls[i].Substring(0, 4);
                            if (template == "{{--")
                            {
                                downloadHtmlsList[downloadHtmlsIndex] = downloadHtmlsList[downloadHtmlsIndex] + downloadHtmls[i];
                            }
                            else if (template.Count(l => l == '\n') > 2)
                            {

                            }
                            else
                            {
                                downloadHtmlsList.Add(downloadHtmls[i]);
                                downloadHtmlsIndex++;
                            }
                        }
                    }
                }

                downloadHtmls = downloadHtmlsList.ToArray();

                string qualityNote = null;

                foreach (var downloadHtml in downloadHtmls)
                {
                    var tmp_downloadHtml = downloadHtml.Replace("&nbsp;", "").TrimStart().TrimEnd();

                    MemoryStream ms = new MemoryStream();
                    TextWriter tw = new StreamWriter(ms);

                    tw.Write(tmp_downloadHtml);
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

                    if (tmpDoc.DocumentNode.Descendants().Count(l => l.Name == "span" || l.Name == "em" || l.Name == "strong" || l.Name == "#text") > 0)
                    {
                        if (!tmpDoc.DocumentNode.InnerText.Contains("{{"))
                            qualityNote = tmpDoc.DocumentNode.InnerText.TrimStart().TrimEnd();
                        else if (tmpDoc.DocumentNode.Descendants()
                                     .Count(l => l.Name == "span" || l.Name == "em" || l.Name == "strong") > 0 && !tmpDoc.DocumentNode.InnerText.Contains("|") && !tmpDoc.DocumentNode.InnerText.Contains("{{"))
                            qualityNote = tmpDoc.DocumentNode.Descendants()
                                .SingleOrDefault(l => l.Name == "span" || l.Name == "em" || l.Name == "strong").InnerText.TrimStart().TrimEnd();

                        tmpDoc.DocumentNode.Descendants().Where(l => l.Name == "em" || l.Name == "span" || l.Name == "strong" &&
                                                                     !tmpDoc.DocumentNode.InnerText.Contains("|") && !tmpDoc.DocumentNode.InnerText.Contains("{{")).ToList().ForEach(l => { l?.Remove(); });

                        if (string.IsNullOrEmpty(tmpDoc.DocumentNode.InnerText.TrimStart().TrimEnd()) || !tmpDoc.DocumentNode.InnerText.Contains("{{"))
                            continue;
                    }

                    var qualityNode = tmpDoc.DocumentNode.Descendants().LastOrDefault(l => l.Name == "b" || l.Name == "strong");
                    var downloadLinkQualityInfo = tmpDoc.DocumentNode.Descendants().LastOrDefault(l => !l.InnerText.Contains("{{"));
                    var downloadLinkInfo = tmpDoc.DocumentNode.Descendants().LastOrDefault(l => l.InnerText.Contains("{{"));
                    var downloadLinkNodes = downloadLinkInfo.InnerText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    var quality1 = qualityNode?.InnerText;
                    quality1 = string.IsNullOrEmpty(quality1) ? null : quality1;
                    var quality2 = downloadLinkNodes.FirstOrDefault(l => !l.Contains("{{"))?
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?
                        .TrimStart()
                        .TrimEnd();
                    quality2 = string.IsNullOrEmpty(quality2) ? null : quality2;
                    var quality3 = downloadLinkQualityInfo?
                        .InnerText
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?
                        .TrimStart()
                        .TrimEnd();
                    quality3 = string.IsNullOrEmpty(quality3) ? null : quality3;
                    bool sizeAvailable = false;
                    var size1 = downloadLinkNodes.FirstOrDefault(l => !l.Contains("{{"))?
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Replace("~", "")
                        .TrimStart()
                        .TrimEnd();
                    size1 = size1 != null && !size1.ToLower().Contains("gb") && !size1.ToLower().Contains("mb") ? null : size1;
                    if (size1 != null && size1.IndexOf("(") > -1)
                        size1 = size1?.Substring(0, size1.IndexOf("("));
                    if (size1 != null && size1.ToLower().Contains("mb") && size1.ToLower().Contains("gb") && size1.Contains("/"))
                    {
                        size1 = size1.ToLower()?.Replace("mb", "");
                        size1 = size1.ToLower()?.Replace("/", "");
                    }

                    float sizeInNumber;
                    sizeAvailable |= size1 != null && float.TryParse(size1.ToLower()
                        .Replace("gb", "")
                        .Replace("mb", "")
                        .Replace("tb", "")
                        .Replace("kb", "")
                        .TrimStart()
                        .TrimEnd(), out sizeInNumber);
                    var size2 = downloadLinkQualityInfo?.InnerText
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?
                        .Replace("~", "")
                        .TrimStart()
                        .TrimEnd();
                    size2 = size2 != null && !size2.ToLower().Contains("gb") && !size2.ToLower().Contains("mb") ? null : size2;
                    if (size2 != null && size2.IndexOf("(") > -1)
                        size2 = size2?.Substring(0, size2.IndexOf("("));
                    if (size2 != null && size2.ToLower().Contains("mb") && size2.ToLower().Contains("gb") && size2.Contains("/"))
                    {
                        size2 = size2?.ToLower().Replace("mb", "");
                        size2 = size2?.ToLower().Replace("/", "");
                    }
                    sizeAvailable |= size2 != null && float.TryParse(size2.ToLower()
                        .Replace("gb", "")
                        .Replace("mb", "")
                        .Replace("tb", "")
                        .Replace("kb", "")
                        .TrimStart()
                        .TrimEnd(), out sizeInNumber);

                    DownloadQuality downloadQuality = new DownloadQuality()
                    {
                        Quality = quality1 ??
                                  quality2 ??
                                  quality3,
                        //Size = sizeAvailable ?
                        //    size1 ?? size2 : null,
                        Notes = qualityNote
                    };

                    if (downloadQuality.Quality == null || downloadQuality.Size == null)
                        downloadQuality.HasError = true;

                    if (qualityNote != null)
                        qualityNote = null;

                    downloadLinkNodes = downloadLinkNodes
                        .Where(l => l.Contains("{{"))
                        .SelectMany(l => l.Split(new[] { "{{" }, StringSplitOptions.RemoveEmptyEntries).Select(s => "{{" + s)) /*Fix Inline Download Links Case*/
                        .Select(l => l.Substring(0, l.LastIndexOf("}}", StringComparison.Ordinal) + 2))
                        .ToArray();

                    foreach (var downloadLinkNode in downloadLinkNodes)
                    {
                        downloadQuality.Links.Add(new Link() { Title = vmMovieLookup.GetByButtonId(downloadLinkNode).ButtonName, Url = vmMovieLookup.GetByButtonId(downloadLinkNode).Url });
                    }

                    tmp_Episode.DownloadQualities.Add(downloadQuality);
                }
            }

            void ProcessDownloadBox(MovieDetails tmp_details, HtmlNode tmp_downloadNode, string tabName)
            {
                MovieEpisode episode = new MovieEpisode() { Title = tabName };

                var downloadInnerNode = tmp_downloadNode.Descendants().SingleByNameNClass("div", "box-inner-block");
                downloadInnerNode.Descendants()
                    .SingleOrDefaultByNameNClass("i", "fa tie-shortcode-boxicon")?.Remove();

                /*Detect HRef Availablity*/
                details.MovieDetailsMode |= downloadInnerNode.Descendants().Any(l =>
                    l.Name == "a" && l.Attributes.Contains("href") && l.Attributes["href"].Value.StartsWith("http")) ? MovieDetailsMode.HRef : MovieDetailsMode.None;

                if (details.MovieDetailsMode.HasFlag(MovieDetailsMode.Obfuscation))
                {
                    HtmlNode shadowNode = downloadInnerNode.CloneNode(true);
                    ProcessVMLinks(episode, shadowNode, tabName);
                }

                if (details.MovieDetailsMode.HasFlag(MovieDetailsMode.HRef))
                {
                    HtmlNode shadowNode = downloadInnerNode.CloneNode(true);
                    ProcessHRefLinks(episode, shadowNode);
                }

                tmp_details.Episodes.Add(episode);
            }

            void ProcessSingleDownloadBox(MovieDetails tmp_details, HtmlNode tmp_docNode, string tabName)
            {
                var downloadNode = tmp_docNode.Descendants().SingleByNameNClass("div", "box download  ");
                ProcessDownloadBox(tmp_details, downloadNode, tabName);
            }

            void ProcessMultipleDownloadBox(MovieDetails tmp_details, HtmlNode tmp_docNode, string tabName)
            {
                var downloadNodes = tmp_docNode.Descendants().FindByNameNClass("div", "box download  ");
                foreach (var downloadNode in downloadNodes)
                {
                    ProcessDownloadBox(tmp_details, downloadNode, tabName);
                }
            }

            void ProcessMultiplePanesBox(MovieDetails tmp_details, HtmlNode tmp_docNode)
            {
                int index = 0;
                var tabsNodes = tmp_docNode
                    .Descendants().FindByNameNClass("ul", "tabs-nav")
                    .SelectMany(l => l.Descendants("li")).ToList();

                if (!tabsNodes.Any())
                {
                    ProcessMultipleDownloadBox(tmp_details, tmp_docNode, null);
                }
                else
                {
                    var paneNodes = docNode.Descendants().FindByNameNClass("div", "pane").Take(tabsNodes.Count());

                    foreach (var paneNode in paneNodes)
                    {
                        var downloadNodes = paneNode.Descendants().FindByNameNClass("div", "box download  ");

                        foreach (var downloadNode in downloadNodes)
                        {
                            string tabName = tabsNodes.Skip(index).Take(1).First().InnerText;
                            ProcessDownloadBox(tmp_details, downloadNode, tabName);
                        }

                        index++;
                    }
                }
            }

            if (docNode.Descendants().CountByNameNClass("div", "box download  ") == 1)
                ProcessSingleDownloadBox(details, docNode, null);
            else if (docNode.Descendants().CountByNameNClass("div", "box download  ") >= 1)
                ProcessMultiplePanesBox(details, docNode);

            return details;
        }

        public static VMMovieLookup DecodeDetailsVM(HtmlDocument document)
        {
            string documentHtml = string.Empty;
            int startIndex = 0;
            int endIndex = 0;
            string startPattern = "";
            string endPattern = "";

            documentHtml = document.ParsedText;

            if (string.IsNullOrEmpty(documentHtml))
                return new VMMovieLookup();

            //Get VM Decoder Parameters
            startPattern = "return decodeURIComponent(escape(r))";
            endPattern = "))";
            startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal) + 2;
            documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
            endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal);
            documentHtml = documentHtml.Substring(0, endIndex);
            documentHtml = documentHtml.Replace("\"", "");
            var vmVariables = documentHtml.Split(new[] { ',' });

            if (vmVariables.Length > 6)
                return new VMMovieLookup();

            var decodedHtml = VMDecoder.eval(vmVariables[0], int.Parse(vmVariables[1]), vmVariables[2], int.Parse(vmVariables[3]),
                int.Parse(vmVariables[4]), int.Parse(vmVariables[5]));

            //Movie Array Id
            documentHtml = decodedHtml;
            startPattern = "location.href=";
            endPattern = "[";
            startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal);
            documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
            endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal);
            documentHtml = documentHtml.Substring(0, endIndex);
            string movieArrayId = documentHtml;

            //Movie Array Object
            documentHtml = decodedHtml;
            startPattern = movieArrayId + "=";
            endPattern = "}; function";
            startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal);
            documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
            endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal) + 1;

            /*Array Case*/
            bool arrayOfString = false;
            if (endIndex == 0)
            {
                endPattern = "]; function";
                endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal) + 1;
                arrayOfString = true;
            }

            documentHtml = documentHtml.Substring(0, endIndex);

            if (string.IsNullOrEmpty(documentHtml))
                return new VMMovieLookup();

            string[] linksArray = null;

            if (arrayOfString)
            {
                /*Array Case*/
                if (documentHtml != "[]")
                {
                    JArray linksInArrayStruct = new JArray(documentHtml);
                    linksArray = linksInArrayStruct.Children().Select(l => l.Value<string>()).ToArray();
                }
            }
            else
            {
                /*Object Case*/
                JObject linksObject = JObject.Parse(documentHtml);
                IEnumerable<JToken> linksTokens = linksObject.Properties().Select(l => l.Value).ToArray();
                linksArray = linksTokens.Select(l => l.Value<string>()).ToArray();
            }

            /*Empty Array => Empty Lookup*/
            if (linksArray == null)
                return new VMMovieLookup();

            //Movie Page Links Buttons
            documentHtml = decodedHtml;
            startPattern = "if (counter== 0){";
            endPattern = "} else {";
            startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal) + 1;
            documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
            endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal) - 1;
            documentHtml = documentHtml.Substring(0, endIndex);
            string[] buttonsHtmlArray = documentHtml.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            /*Merge Non Split Errors For Unicode*/
            var buttonsHtmlList = new List<string>();
            int buttonsHtmlListIndex = -1;
            for (var i = 0; i < buttonsHtmlArray.Length; i++)
            {
                if (buttonsHtmlArray[i].Contains("a.innerHTML"))
                {
                    buttonsHtmlList.Add(buttonsHtmlArray[i]);
                    buttonsHtmlListIndex++;
                }
                else
                {
                    buttonsHtmlList[buttonsHtmlListIndex] += ";" + buttonsHtmlArray[i];
                }
            }

            buttonsHtmlArray = buttonsHtmlList.ToArray();

            var buttonsObjects = buttonsHtmlArray.Select(l =>
            {
                documentHtml = l;
                startPattern = "a.innerHTML=a.innerHTML.replace(`";
                endPattern = "`,";
                startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal);
                documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
                endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal);
                documentHtml = documentHtml.Substring(0, endIndex);
                string buttonId = documentHtml;

                documentHtml = l;
                startPattern = "\">";
                endPattern = "</";
                startIndex = documentHtml.IndexOf(startPattern, StringComparison.Ordinal);
                documentHtml = documentHtml.Substring(startIndex + startPattern.Length, documentHtml.Length - startPattern.Length - startIndex);
                endIndex = documentHtml.IndexOf(endPattern, StringComparison.Ordinal);
                documentHtml = documentHtml.Substring(0, endIndex);
                var buttonName = documentHtml.TrimStart().TrimEnd();

                return new
                {
                    ButtonId = buttonId,
                    ButtonName = buttonName
                };
            }).ToArray();

            VMMovieLookup vmMovieLookup = new VMMovieLookup();
            for (int i = 0; i < linksArray.Length; i++)
            {
                vmMovieLookup.Add(linksArray[i], buttonsObjects[i].ButtonId, buttonsObjects[i].ButtonName);
            }
            return vmMovieLookup;
        }

        public static WebRequestHeader BypassSurcuri(IWebDriver driver, int currentWindow, string[] windows, Semaphore semaphore)
        {
            int timeout = 30; /*sec*/
            
            var bodyWaiter = new WebDriverWait(driver.SwitchTo().Window(windows[currentWindow]), TimeSpan.FromSeconds(timeout));
            var bodyElement = bodyWaiter.Until(ExpectedConditions.ElementExists(new ByAll(By.TagName("body"), By.Id("top"))));

            Thread.Sleep(1000);

            if (bodyElement != null)
                return WebDriverHelper.ReplicateRequestHeader(driver.SwitchTo().Window(windows[currentWindow]));

            ConsoleHelper.LogError("Cannot Bypass Surcuri");
            return null;
        }

        public static string MoviesTrueLinks(IWebDriver driver, int currentWindow, string[] windows, Semaphore semaphore)
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
                    //Case 7
                    (pageSource.Contains("503") && pageSource.Contains("service") &&
                     pageSource.Contains("temporarily") && pageSource.Contains("unavailable") && pageSource.Contains("tengine")) ||
                    //Case 8
                    (pageSource.Contains("bad request"))
                )
                {
                    foreach (var handle in handles)
                        if (handle != windows[currentWindow])
                            driver.SwitchTo().Window(handle).Close();

                    handles.Clear();
                    handles.Push(windows[currentWindow]);

                    throw new Exception("Website is down");
                }

                semaphore.Release();

                if (url.Contains("lonelymoon") || url.Contains("intercelestial") || url.Contains("sweetlantern"))
                {
                    semaphore.WaitOne();

                    if (driver.SwitchTo().Window(handles.Peek()).PageSource.Contains("Redirect to nowhere"))
                    {
                        foreach (var handle in handles)
                            if (handle != windows[currentWindow])
                                driver.SwitchTo().Window(handle).Close();

                        handles.Clear();
                        handles.Push(windows[currentWindow]);

                        semaphore.Release();

                        return "Link Expired";
                    }

                    semaphore.Release();

                    //if (driver.SwitchTo().Window(handles.Peek()).PageSource.Contains("Please verify that you are human"))
                    {
                        retry1:
                        semaphore.WaitOne();

                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("div"), By.ClassName("wait")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                goto retry1;
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
                        retry2:
                        semaphore.WaitOne();

                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("a"), By.Id("generater")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                goto retry2;
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
                            semaphore.Release();
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
                        retry3:
                        semaphore.WaitOne();

                        var elements = driver.SwitchTo().Window(handles.Peek())
                            .FindElements(new ByAll(By.TagName("img"), By.Id("showlink")));

                        if (elements.Count == 0)
                        {
                            if (DateTime.Now.Subtract(loginTime).TotalSeconds < taskTimeout)
                            {
                                semaphore.Release();
                                Thread.Sleep(10);
                                goto retry3;
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

                        semaphore.Release();
                    }
                }

                else if (url.Contains("spacetica") || url.Contains("linegee"))
                {
                    semaphore.WaitOne();

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

                    semaphore.Release();

                    retry4:
                    semaphore.WaitOne();

                    var elements = driver.SwitchTo().Window(handles.Peek()).FindElements(new ByAll(By.TagName("a"), By.LinkText("Continue")));

                    if (elements.Count == 0)
                    {
                        /*Need Unlock*/
                        if (driver.SwitchTo().Window(handles.Peek())
                                .FindElements(new ByAll(By.TagName("button"))).Count(l => l.Text == "Unlock") == 1)
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
                            goto retry4;
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

                else if (url == "about:blank" || String.IsNullOrEmpty(url) || url.Contains("pahe.ph"))
                {
                    semaphore.WaitOne();

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

                    semaphore.Release();
                }

                else
                {
                    semaphore.WaitOne();

                    var otherUrl = url;

                    foreach (var handle in handles)
                        if (handle != windows[currentWindow])
                            driver.SwitchTo().Window(handle).Close();

                    handles.Clear();
                    handles.Push(windows[currentWindow]);

                    semaphore.Release();

                    return otherUrl;
                }

                semaphore.WaitOne();
                url = driver.SwitchTo().Window(handles.Peek()).Url;
                semaphore.Release();
            }
        }
    }
}