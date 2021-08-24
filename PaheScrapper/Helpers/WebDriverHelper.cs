using System.Net;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using PaheScrapper.Helpers;

namespace PaheScrapper.Helpers
{
    public class WebRequestHeader
    {
        public string Accept { get; set; }

        public string AcceptEncoding { get; set; }

        public string AcceptLanguage { get; set; }

        public string CacheControl { get; set; }
        public string Cookie { get; set; }

        public string FNoneMatch { get; set; }
        public string Referer { get; set; }

        public string SecChUa { get; set; }

        public string SecChUaMobile { get; set; }

        public string SecFetchDest { get; set; }

        public string SecFetchMode { get; set; }

        public string SecFetchSite { get; set; }

        public string SecFetchUser { get; set; }

        public string UpgradeInsecureRequests { get; set; }

        public string UserAgent { get; set; }

        public void Set(HttpWebRequest request)
        {
            request.Accept = Accept;
            request.UserAgent = UserAgent;
            request.Referer = Referer;
            //request.Headers["accept-encoding"] = AcceptEncoding;
            //request.Headers["accept-language"] = AcceptLanguage;
            request.Headers["cache-control"] = CacheControl;
            request.Headers["f-none-match"] = FNoneMatch;
            request.Headers["sec-ch-ua"] = SecChUa;
            request.Headers["sec-ch-ua-mobile"] = SecChUaMobile;
            request.Headers["sec-fetch-dest"] = SecFetchDest;
            request.Headers["sec-fetch-mode"] = SecFetchMode;
            request.Headers["sec-fetch-site"] = SecFetchSite;
            request.Headers["sec-fetch-user"] = SecFetchUser;
            request.Headers["upgrade-insecure-requests"] = UpgradeInsecureRequests;
            request.Headers["cookie"] = Cookie;
        }
    }

    public static class WebDriverHelper
    {
        public static WebRequestHeader ReplicateRequestHeader(IWebDriver driver)
        {
            var cookie = driver.Manage().Cookies.AllCookies[0];

            WebRequestHeader header = new WebRequestHeader()
            {
                Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
                AcceptEncoding = "gzip, deflate, br",
                AcceptLanguage = "en-US,en;q=0.9",
                CacheControl = "max-age=0",
                FNoneMatch = "8219014-1629784521;br",
                Referer = "https://pahe.ph/",
                SecChUa = "\"Chromium\";v=\"92\", \" Not A; Brand\";v=\"99\", \"Google Chrome\";v=\"92\"",
                SecChUaMobile = "?0",
                SecFetchDest = "document",
                SecFetchMode = "navigate",
                SecFetchSite = "same-origin",
                SecFetchUser = "?1",
                UpgradeInsecureRequests = "1",
                UserAgent = driver.ExecuteJavaScript<string>("return navigator.userAgent"),
                Cookie = $"{cookie.Name}={cookie.Value}"
            };

            return header;
        }
    }
}