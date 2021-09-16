using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PaheScrapper.Exceptions;
using PaheScrapper.Helpers;
using PaheScrapper.Properties;

namespace PaheScrapper
{
    public static class ScrapperWeb
    {
        static ChromeDriverService service;
        static ChromeOptions options;
        static ChromeDriver driver;
        static string[] windows;
        static Process[] chromeProcesses;
        static Semaphore semaphore;


        public static HtmlDocument GetDownloadHtml(string url, WebRequestHeader header)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            HttpWebResponse response;

            request.Timeout = ScrapperConstants.HttpRequestTimeout();

            header?.Set(request);

            try
            {
                response = (HttpWebResponse) request.GetResponse();
            }
            catch (WebException e)
            {
                throw new ScrapperDownloaderException("Cannot Get Response.", e);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                HtmlDocument doc = new HtmlDocument();

                try
                {
                    doc.Load(response.GetResponseStream());

                    return doc;
                }
                catch (Exception e)
                {
                    throw new ScrapperDownloaderException("Cannot Load Html Stream.", e);
                }
            }

            throw new ScrapperDownloaderException("Cannot Find Proper Response.");
        }

        public static HtmlDocument PostDownloadHtml(string url, Dictionary<string, string> parameters, WebRequestHeader header)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response;

            string postData = String.Empty;

            foreach (var parameter in parameters)
            {
                postData += $"{parameter.Key}={parameter.Value}&";
            }

            postData = postData.Substring(0, postData.Length - 1);

            byte[] data = Encoding.ASCII.GetBytes(postData);

            request.Timeout = ScrapperConstants.HttpRequestTimeout();
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            header?.Set(request);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception e)
            {
                throw new ScrapperDownloaderException("Cannot Get Response.", e);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                HtmlDocument doc = new HtmlDocument();

                try
                {
                    doc.Load(response.GetResponseStream());

                    return doc;
                }
                catch (Exception e)
                {
                    throw new ScrapperDownloaderException("Cannot Load Html Stream.", e);
                }
            }

            throw new ScrapperDownloaderException("Cannot Find Proper Response.");
        }

        public static void InitializeActiveScrape(int clonesNo)
        {
            service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.HideCommandPromptWindow = true;

            options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
            //options.AddUserProfilePreference("profile.default_content_setting_values.cookies", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.popups", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.media_stream", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 2);
            //options.AddUserProfilePreference("profile.default_content_setting_values.app_banner", 2);
            if (Configuration.Default.WebDriveHeadless) options.AddArgument("headless");
            options.AddArgument("incognito");

            driver = new ChromeDriver(service, options);
            driver.Manage().Window.Maximize();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            windows = new string[clonesNo];

            windows[0] = driver.WindowHandles.Last();

            for (int i = 1; i < windows.Length; i++)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("window.open();");
                windows[i] = driver.WindowHandles.Last();
            }

            List<Process> chromeProcessesList = new List<Process>();

            Process[] driverProcesses =
                Process.GetProcesses().Where(l => l.ProcessName.Contains("chromedriver")).ToArray();

            foreach (var driverProcess in driverProcesses)
            {
                chromeProcessesList.AddRange(driverProcess.GetChildProcesses());
            }

            chromeProcesses = chromeProcessesList.ToArray();

            semaphore = new Semaphore(1, 1);
        }

        public static void ReleaseActiveScrape()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();

                Process[] driverProcesses =
                    Process.GetProcesses().Where(l => l.ProcessName.Contains("chromedriver")).ToArray();

                foreach (var driverProcess in driverProcesses)
                {
                    driverProcess.Kill();
                }

                foreach (var chromeProcess in chromeProcesses)
                {
                    if (!chromeProcess.HasExited)
                        chromeProcess.Kill();
                }
            }
        }

        public static void ReleaseGarbageScrape()
        {
            List<Process> chromeProcessesList = new List<Process>();

            Process[] driverProcesses =
                Process.GetProcesses().Where(l => l.ProcessName.Contains("chromedriver")).ToArray();

            foreach (var driverProcess in driverProcesses)
            {
                chromeProcessesList.AddRange(driverProcess.GetChildProcesses());
            }

            chromeProcesses = chromeProcessesList.ToArray();

            foreach (var driverProcess in driverProcesses)
            {
                driverProcess.Kill();
            }

            foreach (var chromeProcess in chromeProcesses)
            {
                try
                {
                    if (!chromeProcess.HasExited)
                        chromeProcess.Kill();
                }
                catch { }
            }
        }


        public static TResult ActiveScrape<TResult>(int index, string url, Func<IWebDriver, int, string[], Semaphore, TResult> controller)
        {
            try
            {
                semaphore.WaitOne();
                driver.SwitchTo().Window(windows[index]).Url = url;
                semaphore.Release();
                var result = controller(driver, index, windows, semaphore);
                return result;
            }
            catch (Exception e)
            {
                semaphore.WaitOne(1);
                semaphore.Release();
                throw;
            }
        }
    }
}