using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WordPressPoster.Models;

namespace WordPressPoster.Helpers
{
    public static class OpenRefineLoader
    {
        public static IEnumerable<OpenRefineItem> Load(string filePath)
        {
            OpenRefineCollection collection;

            using (var f = File.OpenText(filePath))
            {
                var json = f.ReadToEnd();
                collection = JsonConvert.DeserializeObject<OpenRefineCollection>(json);
                f.Close();
            }

            return collection.Items;
        }
    }
}