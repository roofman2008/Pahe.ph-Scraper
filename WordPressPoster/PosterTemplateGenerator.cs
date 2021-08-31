using System;
using System.IO;
using System.Text;
using WordPressPoster.Models;

namespace WordPressPoster
{
    public static class PosterTemplateGenerator
    {
        private static readonly string StaticTemplate;
        private static readonly string TabTemplate;
        private static readonly string SectionTemplate;
        private static readonly string DownloadTemplate;

        static PosterTemplateGenerator()
        {
            StaticTemplate = File.ReadAllText(@"Templates\StaticTemplate.txt");
            TabTemplate = File.ReadAllText(@"Templates\TabTemplate.txt");
            SectionTemplate = File.ReadAllText(@"Templates\SectionTemplate.txt");
            DownloadTemplate = File.ReadAllText(@"Templates\DownloadTemplate.txt");
        }

        public static string GenerateContent(this WordpressPostModel post)
        {
            StringBuilder tabSB = new StringBuilder();

            var staticTemplate = StaticTemplate;
            staticTemplate = staticTemplate.Replace("{title}", post.IMDBName);
            staticTemplate = staticTemplate.Replace("{imdbLink}", post.IMDBLink);
            staticTemplate = staticTemplate.Replace("{youtubeurl}", post.Trailer);
            staticTemplate = staticTemplate.Replace("{screenshotLink}", post.Screenshot);
            staticTemplate = staticTemplate.Replace("{subtitlesLink}", post.Substitles);

            foreach (var tab in post.Tabs)
            {
                StringBuilder sectionSB = new StringBuilder();
                var tabTemplate = TabTemplate;

                tabTemplate = tabTemplate.Replace("{tabname}", tab?.Name ?? "Default");

                foreach (var section in tab.Sections)
                {
                    StringBuilder downloadSB = new StringBuilder();
                    var sectionTemplate = SectionTemplate;

                    sectionTemplate = sectionTemplate.Replace("{sectionname}", section?.Name ?? "Default");

                    foreach (var download in section.Downloads)
                    {
                        var downloadTemplate = DownloadTemplate;

                        downloadTemplate = downloadTemplate.Replace("{downloadUrl}", download.Url);
                        downloadTemplate = downloadTemplate.Replace("{downloadName}", download.Name);
                        downloadTemplate = downloadTemplate.Replace("{color}", GetUrlColor(download.Name));

                        downloadSB.AppendLine(downloadTemplate);
                    }

                    sectionTemplate = sectionTemplate.Replace("[DynamicContent]", downloadSB.ToString());
                    sectionSB.AppendLine(sectionTemplate);
                }

                tabTemplate = tabTemplate.Replace("[DynamicContent]", sectionSB.ToString());
                tabSB.Append(tabTemplate);
            }

            staticTemplate = staticTemplate.Replace("[DynamicContent]", tabSB.ToString());

            return staticTemplate;
        }

        public static string GenerateExcerpt(this WordpressPostModel post)
        {
            return String.Empty;
        }

        private static string GetUrlColor(string name)
        {
            switch (name.ToLower())
            {
                case "mg":
                    return "d60909";
                case "gd":
                    return "1bd606";
                case "utb":
                case "utb1":
                case "utb2":
                case "utb3":
                case "utb4":
                case "utb5":
                case "utb6":
                case "utb7":
                case "utb8":
                case "utb9":
                case "utb10":
                    return "131713";
                case "1d":
                    return "1863cc";
                case "ol":
                    return "4e84cf";
                default:
                    return "000000";
            }
        }
    }
}