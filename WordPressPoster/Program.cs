namespace WordPressPoster
{
    class Program
    {
        static void Main(string[] args)
        {
            PosterManager posterManager = new PosterManager();
            posterManager.Run(@"C:\Users\a84030342\Downloads\manager_dump-json.txt");           
        }
    }
}
