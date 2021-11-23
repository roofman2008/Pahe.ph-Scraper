using HtmlAgilityPack;

namespace PaheScrapper.Helpers
{
    public static class PageErrorReporter
    {
        public static void HtmlError(HtmlDocument document)
        {
            LogWriter lw = new LogWriter("Html Dump: \n" + StringCompressor.CompressString(document.DocumentNode.InnerHtml));
        }
    }
}