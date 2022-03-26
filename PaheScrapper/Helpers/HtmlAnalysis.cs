using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PaheScrapper.NativeModels;

namespace PaheScrapper.Helpers
{
    public static class HtmlAnalysis
    {
        private static readonly string[] Delimiters = {"|", "\n"};
        private static readonly string[] Qualities = { "480p", "720p", "1080p", "2160p" };
        private static readonly string[] VideoEncoding = { "x264", "x265" };
        private static readonly string[] VideoRange = { "10-Bit" };
        private static readonly string[] AudioEncoding = { "DD+7.1" };
        private static readonly string[] Sizes = {"KB", "MB", "GB", "TB" };
        private static readonly string[] Http = {"http", "https"};
        private static readonly string[] HReference = {"<a?>", "</a>"};
        private static readonly string[] FileProviders = { "RCT", "UTB", "MG", "ZS", "1D", "GD", "1F" };
        private static readonly string[] DownloadGroup = { "Episode", "Season", "Per Episode" };
        private static readonly string[] Count = { "Eps" };
        private static readonly string[] UnwantedTags =
        {
            "<span?>", "<b>", "</b>", "</span>", "<br>", "&nbsp;", @"target=""?""",
            @"class=""?""", @"rel=""?""", "<strong>", "</strong>", "<a style=?>", "HDR</a>"
        };

        private static bool ContainPattern(this string source, string[] patterns, out string foundPattern)
        {
            foreach (var pattern in patterns)
            {
                if (!source.ToLower().Contains(pattern.ToLower()))
                    continue;

                foundPattern = pattern;
                return true;
            }

            foundPattern = null;
            return false;
        }
        private static bool ContainPattern(this string source, string[] patterns)
        {
            return patterns.Any(pattern => source.ToLower().Contains(pattern.ToLower()));
        }
        private static bool ContainOpenTags(this string source, string startKey, string endKey)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            var startIndex = source.IndexOf(startKey, StringComparison.Ordinal);

            if (startIndex == -1)
                return false;

            var endIndex = source.IndexOf(endKey, startIndex + startKey.Length, StringComparison.Ordinal);

            return endIndex > startIndex;
        }
        private static string ReplaceOpenTags(this string source, string startKey, string endKey)
        {
            string result = source;

            if (string.IsNullOrEmpty(result))
                return result;

            do
            {
                var startIndex = result.IndexOf(startKey, StringComparison.Ordinal);

                if (startIndex == -1)
                    break;

                var endIndex = result.IndexOf(endKey, startIndex + startKey.Length, StringComparison.Ordinal);
                var template = result.Substring(startIndex, endIndex - startIndex + 1);
                result = result.Replace(template, "");
            } while (result.Contains(startKey));

            return result;
        }
        private static string ReplaceOpenTags(this string source, string[] patterns)
        {
            string result = source;

            foreach (var pattern in patterns)
            {
                if (!pattern.Contains("?"))
                    result = result.Replace(pattern, "");
                else
                {
                    var keys = pattern.Split(new[] {"?"}, StringSplitOptions.RemoveEmptyEntries);
                    result = result.ReplaceOpenTags(keys[0], keys[1]);
                }
            }

            return result;
        }
        private static bool ContainOpenTags(this string source, string[] patterns)
        {
            bool result = true;

            foreach (var pattern in patterns)
            {
                if (!pattern.Contains("?"))
                    result &= source.Contains(pattern);
                else
                {
                    var keys = pattern.Split(new[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
                    result &= source.ContainOpenTags(keys[0], keys[1]);
                }
            }

            return result;
        }

        public static string ExtractSize(string source)
        {
            var tokens = source.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var hasSize = token.ContainPattern(Sizes, out var sizePattern);
                var hasHttp = token.ContainPattern(Http);
                var hasHRef = token.ContainOpenTags(HReference);
                //var hasNote = token.ContainPattern(Notes);

                if (!hasSize || hasHttp || hasHRef /*|| hasNote*/) 
                    continue;

                float size;
                var hasSizeNumber = float.TryParse(token.ToLower().Replace(sizePattern.ToLower(), "").TrimStart().TrimEnd(), out size);

                if (!hasSizeNumber)
                    continue;

                var unit = sizePattern;
                return $"{size} {unit}";
            }

            return null;
        }
        public static string ExtractQuality(string source)
        {
            var tokens = source.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var hasQuality = token.ContainPattern(Qualities);
                var hasHttp = token.ContainPattern(Http);
                var hasHRef = token.ContainOpenTags(HReference);
                //var hasNote = token.ContainPattern(Notes);

                if (!hasQuality || hasHttp || hasHRef /*|| hasNote*/)
                    continue;

                var quality = token.TrimStart().TrimEnd();
                return quality;
            }

            return null;
        }
        //public static string ExtractNote(string source)
        //{
        //    var tokens = source.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var token in tokens)
        //    {
        //        var hasNote = token.ContainPattern(Notes);
        //        var hasHttp = token.ContainPattern(Http);
        //        var hasHRef = token.ContainOpenTags(HReference);

        //        if (!hasNote || hasHttp || hasHRef)
        //            continue;

        //        var note = token.TrimStart().TrimEnd();
        //        return note;
        //    }

        //    return null;
        //}
        public static HRef[] ExtractHRef(string source)
        {
            var hRefs = new List<HRef>();

            var tokens = source.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var hasHttp = token.ContainPattern(Http);

                if (!hasHttp)
                    continue;

                string url = null;
                string provider = null;

                var hrefTokens = token.Split(HReference, StringSplitOptions.RemoveEmptyEntries);

                foreach (var hrefToken in hrefTokens)
                {
                    var hasHRef = hrefToken.ContainPattern(Http);
                    hrefToken.ContainPattern(FileProviders, out provider);

                    if (hasHRef)
                        url = hrefToken.TrimStart().TrimEnd();
                }

                if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(provider))
                    hRefs.Add(new HRef()
                    {
                        Title = provider,
                        Url = url
                    });
            }

            return hRefs.ToArray();
        }
        public static string CleanHtml(string source)
        {
            return source.ReplaceOpenTags(UnwantedTags);
        }
        public static string[] LinesToArray(string source)
        {
            return source.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}