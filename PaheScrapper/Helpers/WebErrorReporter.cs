using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace PaheScrapper.Helpers
{
    public static class WebErrorReporter
    {
        public static void WebException(WebException webException)
        {
            LogWriter lw = new LogWriter("Web Exception Dump: \n" + JsonConvert.SerializeObject(webException));
        }

        public static void HtmlError(HtmlDocument htmlDocument)
        {
            LogWriter lw = new LogWriter("Html Dump: \n" + StringCompressor.CompressString(htmlDocument.DocumentNode.InnerHtml));
        }
    }
}