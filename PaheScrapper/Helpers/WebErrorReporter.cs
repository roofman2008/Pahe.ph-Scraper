using System.Net;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace PaheScrapper.Helpers
{
    public static class WebErrorReporter
    {
        public static void HttpError(WebResponse webResponse)
        {
            LogWriter lw = new LogWriter("Http Dump: \n" + JsonConvert.SerializeObject(webResponse));
        }

        public static void HtmlError(HtmlDocument htmlDocument)
        {
            LogWriter lw = new LogWriter("Html Dump: \n" + StringCompressor.CompressString(htmlDocument.DocumentNode.InnerHtml));
        }
    }
}