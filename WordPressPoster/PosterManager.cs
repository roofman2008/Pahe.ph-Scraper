using System;
using System.Linq;
using WordPressPoster.Helpers;
using WordPressRestApi.CreateModel;
using WordPressRestApi.Models;

namespace WordPressPoster
{
    public class PosterManager
    {
        public void Run(string file)
        {
            var models = OpenRefineLoader.Load(file);

            var transformed = models.ToWordpressModel().Take(1);

            var x = transformed.First().GenerateContent();

            foreach (var transform in transformed)
            {
                var downloads = transform.Tabs.SelectMany(l => l.Sections.SelectMany(ll => ll.Downloads)).ToList();

                foreach (var download in downloads)
                {
                    download.SafeId = SafeLinkHelper.AddLink(download.Url);
                }

                string title = transform.Title;
                string content = transform.GenerateContent();
                string excerpt = transform.GenerateExcerpt();

                transform.PostId = WordpressHelper.CreatePost(new PostCreate()
                {
                    Title = title,
                    DateGmt = DateTime.Now,
                    Date = DateTime.Now,
                    Tags = transform.Tags.ToList(),
                    Content = content,
                    Excerpt = excerpt,
                    Status = PostStatus.PUBLISH,
                    Author = 0,
                    Categories = null,
                }).Id;
            }
        }
    }
}