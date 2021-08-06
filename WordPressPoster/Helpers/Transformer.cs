using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WordPressPoster.Models;

namespace WordPressPoster.Helpers
{
    public static class Transformer
    {
        public static IEnumerable<WordpressPostModel> ToWordpressModel(this IEnumerable<OpenRefineItem> model)
        {
            var postModelGroup = model.GroupBy(l => new
            {
                l.Title,
                l.Tags,
                l.Trailer,
                l.Screenshot,
                l.Subscense,
                l.IMDBName,
                l.IMDBUrl
            });
            var posts = new List<WordpressPostModel>();

            foreach (var postGroupItem in postModelGroup)
            {
                WordpressPostModel post = new WordpressPostModel()
                {
                    Title = postGroupItem.Key.Title,
                    Tags = postGroupItem.Key.Tags.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries),
                    Screenshot = postGroupItem.Key.Screenshot,
                    Trailer = postGroupItem.Key.Trailer,
                    Substitles = postGroupItem.Key.Subscense,
                    IMDBName = postGroupItem.Key.IMDBName,
                    IMDBLink = postGroupItem.Key.IMDBUrl
                };

                var tabModelGroup = postGroupItem.GroupBy(l => l.Tab);
                var tabs = new List<WordpressTabModel>();

                foreach (var tabGroupItem in tabModelGroup)
                {
                    WordpressTabModel tab = new WordpressTabModel()
                    {
                        Name = tabGroupItem.Key
                    };

                    var sectionModelGroup = tabGroupItem.GroupBy(l => l.Section);
                    var sections = new List<WordpressSectionModel>();

                    foreach (var sectionGroupItem in sectionModelGroup)
                    {
                        WordpressSectionModel section = new WordpressSectionModel()
                        {
                            Name = sectionGroupItem.Key,
                        };

                        var downloads = new List<WordpressDownloadModel>();

                        foreach (var downloadItem in sectionGroupItem)
                        {
                            downloads.Add(new WordpressDownloadModel()
                            {
                                Name = downloadItem.DownloadTitle,
                                Url = downloadItem.DownloadUrl
                            });
                        }

                        section.Downloads = downloads;
                        sections.Add(section);
                    }

                    tab.Sections = sections;
                    tabs.Add(tab);
                }

                post.Tabs = tabs;
                posts.Add(post);
            }

            return posts;
        }
    }
}