using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordPressPoster.Helpers;
using WordPressRestApi;
using WordPressRestApi.CreateModel;

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
