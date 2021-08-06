namespace PaheScrapper
{
    public static class ScrapperConstants
    {
        public static string WebsiteLanding() => "https://pahe.ph/";
        public static string WebsiteLandingPaging(int page) => page <= 1 ? WebsiteLanding() : $"https://pahe.ph/page/{page}/";
        public static int HttpRequestTimeout() => 15 * 1000;
    }
}