using System;

namespace PaheScrapper.Helpers
{
    public static class UriHelper
    {
        public static string GetHost(this string url)
        {
            try
            {
                Uri myUri = new Uri(url);
                string host = myUri.Host;
                return host;
            }
            catch (Exception e)
            {
                return url;
            }          
        }
    }
}