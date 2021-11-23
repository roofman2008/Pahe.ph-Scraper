using HtmlAgilityPack;

namespace PaheScrapper.Helpers
{
    public static class WebErrorReporter
    {
        public static void HttpError(HtmlDocument document)
        {
            LogWriter lw = new LogWriter("Http Dump: \n" + StringCompressor.CompressString(document.DocumentNode.InnerHtml));
        }

        public static void HtmlError(HtmlDocument document)
        {
            LogWriter lw = new LogWriter("Html Dump: \n" + StringCompressor.CompressString(document.DocumentNode.InnerHtml));
        }
    }
}