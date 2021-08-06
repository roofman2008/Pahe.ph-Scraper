using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace PaheScrapper
{
    public static class ScrapperExentions
    {
        public static int CountByNameNClass<TSource>(this IEnumerable<TSource> source, string name, string @class) where TSource : HtmlNode
        {
            return source.Count(l =>
                l.Name == name && l.Attributes.Contains("class") && l.Attributes["class"].Value == @class);
        }

        public static TSource SingleByNameNClass<TSource>(this IEnumerable<TSource> source, string name, string @class) where TSource : HtmlNode
        {
            return source.Single(l =>
                l.Name == name && l.Attributes.Contains("class") && l.Attributes["class"].Value == @class);
        }

        public static TSource SingleOrDefaultByNameNClass<TSource>(this IEnumerable<TSource> source, string name, string @class) where TSource : HtmlNode
        {
            return source.SingleOrDefault(l =>
                l.Name == name && l.Attributes.Contains("class") && l.Attributes["class"].Value == @class);
        }

        public static IEnumerable<TSource> FindByNameNClass<TSource>(this IEnumerable<TSource> source, string name, string @class) where TSource : HtmlNode
        {
            return source.Where(l =>
                l.Name == name && l.Attributes.Contains("class") && l.Attributes["class"].Value == @class);
        }
    }
}